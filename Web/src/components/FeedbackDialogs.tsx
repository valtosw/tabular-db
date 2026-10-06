import { AlertTriangle, Check, X } from 'lucide-react';
import { Dialog } from './Dialog';

interface ErrorDialogProps {
  message: string;
  onClose: () => void;
}

export function ErrorDialog({ message, onClose }: ErrorDialogProps) {
  return (
    <Dialog title="Something needs your attention" onClose={onClose}>
      <div className="feedback-content">
        <span className="feedback-icon warning"><AlertTriangle size={20} /></span>
        <p>{message}</p>
      </div>
      <footer className="dialog-actions">
        <button className="button button-primary" type="button" onClick={onClose}>
          <Check size={16} /> Got it
        </button>
      </footer>
    </Dialog>
  );
}

interface NoticeBannerProps {
  message: string;
  onClose: () => void;
}

export function NoticeBanner({ message, onClose }: NoticeBannerProps) {
  return (
    <div className="notice-banner" role="status">
      <span className="notice-check"><Check size={15} /></span>
      <span>{message}</span>
      <button className="icon-button" type="button" aria-label="Dismiss notification" onClick={onClose}>
        <X size={15} />
      </button>
    </div>
  );
}