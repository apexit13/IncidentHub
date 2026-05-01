import { useEffect } from 'react';

interface ToastProps {
  message: string;
  onDone: () => void;
}

export function Toast({ message, onDone }: ToastProps) {
  useEffect(() => {
    const t = setTimeout(onDone, 3500);
    return () => clearTimeout(t);
  }, [onDone]);
  
  return (
    <div className="bg-gray-900 text-white px-4 py-3 rounded-lg shadow-2xl flex items-center gap-2.5 text-sm font-semibold max-w-sm transform transition-all duration-300 ease-in-out hover:scale-105">
      <span className="text-base">📡</span>
      {message}
    </div>
  );
}