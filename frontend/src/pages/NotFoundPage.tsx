import { Link } from "react-router-dom";
import { Icon } from "../components/Icon";

export function NotFoundPage() {
    return (
        <main className="system-page system-page-full">
            <span className="system-page-icon"><Icon name="search" size={28} /></span>
            <p className="system-page-code">404</p>
            <h1>Aradığınız sayfa bulunamadı</h1>
            <p>Bağlantı değişmiş veya sayfa artık kullanılamıyor olabilir.</p>
            <Link className="button button-primary" to="/dashboard">Genel bakışa dön</Link>
        </main>
    );
}
