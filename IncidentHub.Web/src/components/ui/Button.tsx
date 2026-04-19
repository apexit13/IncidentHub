interface ButtonProps {
  children: React.ReactNode;
  onClick?: () => void;
  variant?: 'primary' | 'secondary';
  disabled?: boolean;
  className?: string;
}

export function Button({ children, onClick, variant = 'primary', disabled = false, className }: ButtonProps) {
  const baseClasses = "px-5 py-2 rounded font-semibold text-sm transition-colors cursor-pointer";
  
  const variantClasses = {
    primary: disabled 
      ? "bg-gray-400 cursor-default" 
      : "bg-blue-700 hover:bg-blue-800 text-white",
    secondary: "border border-gray-300 bg-white text-gray-600 hover:bg-gray-50"
  };
  
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`${baseClasses} ${variantClasses[variant]} ${className}`}
    >
      {children}
    </button>
  );
}