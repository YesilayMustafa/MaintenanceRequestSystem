import { Link } from "react-router-dom";
import { Icon } from "../components/Icon";

export function ForbiddenPage() {
    return (
        <div className="system-page">
            <span className="system-page-icon"><Icon name="alert" size={28} /></span>
            <p className="system-page-code">403</p>
            <h1>Bu alana erişiminiz yok</h1>
            <p>Hesabınız bu sayfayı görüntülemek için gerekli yetkiye sahip değil.</p>
            <Link className="button button-primary" to="/dashboard">Genel bakışa dön</Link>
        </div>
    );
}
