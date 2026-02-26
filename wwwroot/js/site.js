// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(document).ready(function () {
    // Restore sidebar scroll position from previous page
    restoreSidebarScrollPosition();

    // Initialize menu state first
    initializeMenuState();

    // Then bind event handlers
    bindMenuEventHandlers();

    // Save sidebar scroll position before navigation
    saveSidebarScrollOnNavigation();
});

function initializeMenuState() {
    // Add active class to current menu item
    const currentPath = window.location.pathname;
    $('.menu-item, .submenu-item, .nested-submenu-item').each(function() {
        const href = $(this).attr('href');
        if (href === currentPath) {
            $(this).addClass('active');
            // If it's a nested submenu item, open its parents
            if ($(this).hasClass('nested-submenu-item')) {
                $(this).closest('.has-nested-submenu').addClass('open');
                $(this).closest('.has-submenu').addClass('open');
            }
            // If it's a submenu item, open its parent
            else if ($(this).hasClass('submenu-item')) {
                $(this).closest('.has-submenu').addClass('open');
            }
        }
    });

    // Check for special cases where nested submenus should be open based on current path
    if (currentPath.includes('/SimRequests') || currentPath.includes('/ICTS')) {
        $('.has-nested-submenu').has('a[href*="/SimRequests"], a[href*="/ICTS"]').addClass('open');
        $('.has-submenu').has('.has-nested-submenu.open').addClass('open');
    }
    
    if (currentPath.includes('/RefundRequests') || currentPath.includes('/BudgetOfficer') || 
        currentPath.includes('/ClaimsUnit') || currentPath.includes('/PaymentApprover')) {
        $('.has-nested-submenu').has('a[href*="/RefundRequests"], a[href*="/BudgetOfficer"], a[href*="/ClaimsUnit"], a[href*="/PaymentApprover"]').addClass('open');
        $('.has-submenu').has('.has-nested-submenu.open').addClass('open');
    }
    
    if (currentPath.includes('/Admin/')) {
        $('.has-nested-submenu').has('a[href*="/Admin/"]').addClass('open');
        $('.has-submenu').has('.has-nested-submenu.open').addClass('open');
    }
}

function bindMenuEventHandlers() {
    // Remove any existing handlers to prevent duplicate bindings
    $('.has-submenu > .menu-item').off('click.menuHandler');
    $('.has-nested-submenu > .submenu-item, .has-nested-submenu > .menu-item').off('click.nestedMenuHandler');
    
    // Handle submenu toggle
    $('.has-submenu > .menu-item').on('click.menuHandler', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        const $parent = $(this).parent();
        const wasOpen = $parent.hasClass('open');
        
        // Close other open submenus (but not nested ones)
        $('.has-submenu.open').not($parent).not($parent.find('.has-submenu')).removeClass('open');
        
        // Toggle current submenu
        $parent.toggleClass('open');
        
        // Only scroll if we're opening the menu
        if (!wasOpen && $parent.hasClass('open')) {
            // Scroll to ensure the expanded menu is visible
            setTimeout(function() {
                scrollToExpandedMenu($parent[0]);
            }, 350); // Wait for animation to complete
        }
    });

    // Handle nested submenu toggle (both submenu-item and menu-item can trigger nested submenus)
    $('.has-nested-submenu > .submenu-item, .has-nested-submenu > .menu-item').on('click.nestedMenuHandler', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        const $parent = $(this).parent();
        const wasOpen = $parent.hasClass('open');
        
        // Close other open nested submenus in the same level
        $parent.siblings('.has-nested-submenu.open').removeClass('open');
        
        // Toggle current nested submenu
        $parent.toggleClass('open');
        
        // Only scroll if we're opening the menu
        if (!wasOpen && $parent.hasClass('open')) {
            // Scroll to ensure the expanded nested menu is visible
            setTimeout(function() {
                scrollToExpandedMenu($parent[0]);
            }, 350); // Wait for animation to complete
        }
    });
}

function scrollToExpandedMenu(parentElement) {
    const sidebar = $('#sidebar')[0];

    // Check if the expanded menu is outside the visible area
    const parentRect = parentElement.getBoundingClientRect();
    const sidebarRect = sidebar.getBoundingClientRect();

    if (parentRect.bottom > sidebarRect.bottom) {
        // Scroll down to show the expanded content
        const scrollAmount = parentRect.bottom - sidebarRect.bottom + 50;
        if (sidebar.scrollTo) {
            sidebar.scrollTo({
                top: sidebar.scrollTop + scrollAmount,
                behavior: 'smooth'
            });
        } else {
            // Fallback for older browsers
            $(sidebar).animate({ scrollTop: sidebar.scrollTop + scrollAmount }, 300);
        }
    }
}

// Save sidebar scroll position to sessionStorage before navigating
function saveSidebarScrollOnNavigation() {
    // Save scroll position when clicking any link in the sidebar
    $('#sidebar a').on('click', function(e) {
        const sidebar = $('#sidebar')[0];
        if (sidebar) {
            sessionStorage.setItem('sidebarScrollPosition', sidebar.scrollTop);
        }
    });

    // Also save when the page is about to unload
    $(window).on('beforeunload', function() {
        const sidebar = $('#sidebar')[0];
        if (sidebar) {
            sessionStorage.setItem('sidebarScrollPosition', sidebar.scrollTop);
        }
    });
}

// Restore sidebar scroll position from sessionStorage
function restoreSidebarScrollPosition() {
    const savedScrollPosition = sessionStorage.getItem('sidebarScrollPosition');
    if (savedScrollPosition !== null) {
        const sidebar = $('#sidebar')[0];
        if (sidebar) {
            // Restore immediately
            sidebar.scrollTop = parseInt(savedScrollPosition);

            // Also restore after a short delay to ensure DOM is fully loaded
            setTimeout(function() {
                sidebar.scrollTop = parseInt(savedScrollPosition);
            }, 50);
        }
    }
}

// Mobile menu toggle functionality
$(document).ready(function() {
    // Toggle mobile menu
    $('#sidebarToggle').on('click', function(e) {
        e.preventDefault();
        $('#sidebar').toggleClass('active');
        $('#sidebarOverlay').toggleClass('active');
        $('body').toggleClass('sidebar-open');
    });
    
    // Close mobile menu when clicking overlay
    $('#sidebarOverlay').on('click', function() {
        $('#sidebar').removeClass('active');
        $('#sidebarOverlay').removeClass('active');
        $('body').removeClass('sidebar-open');
    });
    
    // Close mobile menu when clicking a menu link (for better UX)
    $('#sidebar a').on('click', function() {
        if ($(window).width() <= 1100) {
            // Don't close if it's a submenu toggle
            if (!$(this).parent().hasClass('has-submenu') && 
                !$(this).parent().hasClass('has-nested-submenu')) {
                $('#sidebar').removeClass('active');
                $('#sidebarOverlay').removeClass('active');
                $('body').removeClass('sidebar-open');
            }
        }
    });
    
    // Handle window resize
    $(window).on('resize', function() {
        if ($(window).width() > 1100) {
            $('#sidebar').removeClass('active');
            $('#sidebarOverlay').removeClass('active');
            $('body').removeClass('sidebar-open');
        }
    });
});

// Menu initialization complete

// ========================================================================
// Notification System
// ========================================================================

$(document).ready(function() {
    // Mark notification as read when clicked
    $('.notification-item').on('click', function(e) {
        const notificationId = $(this).data('notification-id');
        const $item = $(this);

        // Only mark as read if it's currently unread
        if ($item.hasClass('unread')) {
            // Mark as read via AJAX
            $.ajax({
                url: '/api/notifications/' + notificationId + '/markasread',
                method: 'POST',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                success: function() {
                    // Remove unread class
                    $item.removeClass('unread');

                    // Update badge count
                    updateNotificationBadge();
                },
                error: function() {
                    console.error('Failed to mark notification as read');
                }
            });
        }
    });

    // Auto-refresh notification count every 30 seconds
    if ($('.notification-bell-btn').length > 0) {
        setInterval(function() {
            updateNotificationBadge();
        }, 30000); // 30 seconds
    }

    // Animate bell when new notifications arrive
    function animateBellIcon() {
        $('.notification-bell-btn i').addClass('bi-bell-fill');
        setTimeout(function() {
            $('.notification-bell-btn i').removeClass('bi-bell-fill').addClass('bi-bell');
        }, 2000);
    }
});

function updateNotificationBadge() {
    $.ajax({
        url: '/api/notifications/unread-count',
        method: 'GET',
        success: function(data) {
            const $badge = $('.notification-badge');
            const currentCount = parseInt($badge.text()) || 0;
            const newCount = data.count || 0;

            if (newCount > 0) {
                if (newCount > currentCount) {
                    // New notification arrived - animate
                    $('.notification-bell-btn').addClass('has-new-notification');
                    setTimeout(function() {
                        $('.notification-bell-btn').removeClass('has-new-notification');
                    }, 2000);
                }

                // Update or create badge
                if ($badge.length) {
                    $badge.text(newCount > 99 ? '99+' : newCount);
                } else {
                    $('.notification-bell-btn').append(
                        '<span class="notification-badge">' +
                        (newCount > 99 ? '99+' : newCount) +
                        '</span>'
                    );
                }
            } else {
                // Remove badge if no unread notifications
                $badge.remove();
            }
        },
        error: function() {
            console.error('Failed to update notification count');
        }
    });
}
