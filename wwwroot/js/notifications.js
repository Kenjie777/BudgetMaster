// ══════════════════════════════════════════════════════════════════════════════════════
// NOTIFICATION SYSTEM
// ══════════════════════════════════════════════════════════════════════════════════════

class NotificationSystem {
    constructor() {
        this.notifBtn = document.getElementById('notifBtn');
        this.notifModal = document.getElementById('notificationModal');
        this.notifBadge = document.getElementById('notifBadge');
        this.notificationList = document.getElementById('notificationList');
        this.markAllReadBtn = document.getElementById('markAllReadBtn');
        this.clearReadBtn = document.getElementById('clearReadBtn');
        
        this.isOpen = false;
        this.notifications = [];
        
        this.init();
    }
    
    init() {
        // Load initial notification count
        this.loadUnreadCount();
        
        // Set up event listeners
        this.notifBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            this.toggleModal();
        });
        
        // Close modal when clicking outside
        document.addEventListener('click', (e) => {
            if (this.isOpen && !this.notifModal.contains(e.target) && !this.notifBtn.contains(e.target)) {
                this.closeModal();
            }
        });
        
        // Mark all as read button
        this.markAllReadBtn.addEventListener('click', () => {
            this.markAllAsRead();
        });
        
        // Clear read notifications button
        this.clearReadBtn.addEventListener('click', () => {
            this.clearReadNotifications();
        });
        
        // Poll for new notifications every 30 seconds
        setInterval(() => {
            this.loadUnreadCount();
        }, 30000);
    }
    
    async toggleModal() {
        if (this.isOpen) {
            this.closeModal();
        } else {
            this.openModal();
        }
    }
    
    async openModal() {
        this.isOpen = true;
        this.notifModal.style.display = 'flex';
        await this.loadNotifications();
    }
    
    closeModal() {
        this.isOpen = false;
        this.notifModal.style.display = 'none';
    }
    
    async loadUnreadCount() {
        try {
            const response = await fetch('/Notification/GetUnreadCount');
            const data = await response.json();
            
            if (data.unreadCount > 0) {
                this.notifBadge.textContent = data.unreadCount > 99 ? '99+' : data.unreadCount;
                this.notifBadge.style.display = 'block';
            } else {
                this.notifBadge.style.display = 'none';
            }
        } catch (error) {
            console.error('Error loading unread count:', error);
        }
    }
    
    async loadNotifications() {
        this.notificationList.innerHTML = '<div class="notif-loading"><i class="fa-solid fa-spinner fa-spin"></i> Loading notifications...</div>';
        
        try {
            const response = await fetch('/Notification/GetNotifications');
            const data = await response.json();
            
            this.notifications = data.notifications;
            this.renderNotifications();
        } catch (error) {
            console.error('Error loading notifications:', error);
            this.notificationList.innerHTML = '<div class="notif-empty"><i class="fa-solid fa-exclamation-circle"></i><p>Failed to load notifications</p></div>';
        }
    }
    
    renderNotifications() {
        if (this.notifications.length === 0) {
            this.notificationList.innerHTML = `
                <div class="notif-empty">
                    <i class="fa-solid fa-bell-slash"></i>
                    <p>No notifications yet</p>
                </div>
            `;
            return;
        }
        
        const html = this.notifications.map(notif => this.renderNotificationItem(notif)).join('');
        this.notificationList.innerHTML = html;
        
        // Add click handlers to notification items
        this.notificationList.querySelectorAll('.notification-item').forEach(item => {
            item.addEventListener('click', () => {
                const notifId = parseInt(item.dataset.id);
                const actionUrl = item.dataset.url;
                this.handleNotificationClick(notifId, actionUrl);
            });
        });
    }
    
    renderNotificationItem(notif) {
        const iconClass = this.getIconClass(notif.type);
        const unreadClass = notif.isRead ? '' : 'unread';
        
        return `
            <div class="notification-item ${unreadClass}" data-id="${notif.id}" data-url="${notif.actionUrl || '#'}">
                <div class="notif-item-header">
                    <div class="notif-item-icon type-${notif.type.toLowerCase()}">
                        <i class="${iconClass}"></i>
                    </div>
                    <div class="notif-item-content">
                        <div class="notif-item-title">${this.escapeHtml(notif.title)}</div>
                        <div class="notif-item-message">${this.escapeHtml(notif.message)}</div>
                        <div class="notif-item-time">
                            <i class="fa-solid fa-clock"></i> ${notif.timeAgo}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }
    
    getIconClass(type) {
        const icons = {
            'Info': 'fa-solid fa-circle-info',
            'Success': 'fa-solid fa-circle-check',
            'Warning': 'fa-solid fa-triangle-exclamation',
            'Error': 'fa-solid fa-circle-xmark'
        };
        return icons[type] || 'fa-solid fa-bell';
    }
    
    async handleNotificationClick(notifId, actionUrl) {
        // Mark as read
        await this.markAsRead(notifId);
        
        // Navigate to action URL if provided
        if (actionUrl && actionUrl !== '#') {
            window.location.href = actionUrl;
        }
    }
    
    async markAsRead(notifId) {
        try {
            const response = await fetch('/Notification/MarkAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ id: notifId })
            });
            
            if (response.ok) {
                // Update UI
                const notifItem = this.notificationList.querySelector(`[data-id="${notifId}"]`);
                if (notifItem) {
                    notifItem.classList.remove('unread');
                }
                
                // Update badge
                await this.loadUnreadCount();
            }
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    }
    
    async markAllAsRead() {
        try {
            const response = await fetch('/Notification/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            if (response.ok) {
                // Update UI
                this.notificationList.querySelectorAll('.notification-item.unread').forEach(item => {
                    item.classList.remove('unread');
                });
                
                // Update badge
                this.notifBadge.style.display = 'none';
                
                // Show success message
                this.showToast('All notifications marked as read', 'success');
            }
        } catch (error) {
            console.error('Error marking all as read:', error);
            this.showToast('Failed to mark notifications as read', 'error');
        }
    }
    
    async clearReadNotifications() {
        if (!confirm('Are you sure you want to clear all read notifications?')) {
            return;
        }
        
        try {
            const response = await fetch('/Notification/ClearRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            if (response.ok) {
                const data = await response.json();
                
                // Reload notifications
                await this.loadNotifications();
                
                // Show success message
                this.showToast(`${data.count} read notification(s) cleared`, 'success');
            }
        } catch (error) {
            console.error('Error clearing read notifications:', error);
            this.showToast('Failed to clear notifications', 'error');
        }
    }
    
    showToast(message, type = 'success') {
        const toast = document.createElement('div');
        toast.className = `alert-toast ${type}`;
        toast.innerHTML = `
            <i class="fa-solid fa-${type === 'success' ? 'circle-check' : 'circle-xmark'}"></i>
            ${message}
        `;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.remove();
        }, 3000);
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize notification system when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.notificationSystem = new NotificationSystem();
});
