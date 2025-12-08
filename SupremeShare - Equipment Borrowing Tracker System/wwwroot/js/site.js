// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ===========================================
// Scroll to Top Button with Progress Ring
// ===========================================
(function() {
    'use strict';
    
    const scrollToTopBtn = document.getElementById('scrollToTop');
    const progressRing = document.getElementById('progressRing');
    
    if (!scrollToTopBtn || !progressRing) return;
    
    // Circle properties (radius 22 for 50x50 viewBox)
    const radius = 22;
    const circumference = 2 * Math.PI * radius; // ~138.23
    
    // Set initial stroke-dasharray
    progressRing.style.strokeDasharray = circumference;
    progressRing.style.strokeDashoffset = circumference;
    
    // Calculate scroll percentage and update progress ring
    function updateScrollProgress() {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const scrollHeight = document.documentElement.scrollHeight - document.documentElement.clientHeight;
        
        // Calculate scroll percentage (0 to 1)
        const scrollPercent = scrollHeight > 0 ? scrollTop / scrollHeight : 0;
        
        // Update progress ring - offset decreases as scroll increases
        const offset = circumference - (scrollPercent * circumference);
        progressRing.style.strokeDashoffset = offset;
        
        // Show/hide button based on scroll position
        if (scrollTop > 300) {
            scrollToTopBtn.classList.add('visible');
        } else {
            scrollToTopBtn.classList.remove('visible');
        }
    }
    
    // Smooth scroll to top
    function scrollToTop() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    }
    
    // Event listeners
    window.addEventListener('scroll', updateScrollProgress, { passive: true });
    scrollToTopBtn.addEventListener('click', scrollToTop);
    
    // Initial check
    updateScrollProgress();
})();
