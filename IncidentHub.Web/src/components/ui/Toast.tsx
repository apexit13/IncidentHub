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
    <div className="fixed bottom-6 right-6 z-2000 bg-gray-900 text-white px-4 py-3 rounded-lg shadow-2xl flex items-center gap-2.5 text-sm font-semibold max-w-sm">
      <span className="text-base">📡</span>
      {message}
    </div>
  );
}