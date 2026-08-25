export function DoodleArrow({ className = "" }: { className?: string }) {
  return (
    <svg width="80" height="30" viewBox="0 0 80 30" fill="none" className={className} xmlns="http://www.w3.org/2000/svg">
      <path d="M5 15 Q 25 5 45 15 T 75 15" stroke="#151613" strokeWidth="2" strokeLinecap="round" fill="none" />
      <path d="M68 8 L75 15 L68 22" stroke="#151613" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" fill="none" />
    </svg>
  )
}

export function DoodleStar({ className = "" }: { className?: string }) {
  return (
    <span className={`inline-block hand text-xl rotate-[-8deg] ${className}`}>✦</span>
  )
}

export function ScribbleCircle({ children, className = "" }: { children: React.ReactNode; className?: string }) {
  return (
    <span className={`relative inline-block ${className}`}>
      <span className="relative z-10">{children}</span>
      <svg className="absolute -inset-2 w-[calc(100%+16px)] h-[calc(100%+16px)] -z-0" viewBox="0 0 100 40" preserveAspectRatio="none">
        <ellipse cx="50" cy="20" rx="48" ry="16" fill="none" stroke="#f7c948" strokeWidth="3" strokeLinecap="round" opacity="0.9" />
        <ellipse cx="50" cy="20" rx="46" ry="14" fill="none" stroke="#151613" strokeWidth="1.2" strokeDasharray="2 3" opacity="0.35" />
      </svg>
    </span>
  )
}
