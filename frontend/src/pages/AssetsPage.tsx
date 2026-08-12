import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";
import { Link } from "react-router-dom";

import {
    changeAssetStatus,
    createAsset,
    getAssets,
    updateAsset,
} from "../api/assetsApi";
import { getDepartments } from "../api/departmentsApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import { ActiveStatusBadge } from "../components/ManagementBadges";

import type {
    AssetDto,
    AssetTypeName,
    AssetTypeValue,
} from "../types/assets";
import type { DepartmentDto } from "../types/departments";

const maxNameLength = 150;
const maxSerialNumberLength = 100;
const maxLocationLength = 200;

const assetTypeOptions: Array<{
    label: string;
    name: AssetTypeName;
    value: AssetTypeValue;
}> = [
    { label: "Bilgisayar", name: "Computer", value: 1 },
    { label: "Yazıcı", name: "Printer", value: 2 },
    { label: "Sunucu", name: "Server", value: 3 },
    { label: "Ağ Cihazı", name: "NetworkDevice", value: 4 },
    { label: "Yazılım Sistemi", name: "SoftwareSystem", value: 5 },
    { label: "Diğer", name: "Other", value: 6 },
];

const assetTypeValues: Record<AssetTypeName, AssetTypeValue> = {
    Computer: 1,
    Printer: 2,
    Server: 3,
    NetworkDevice: 4,
    SoftwareSystem: 5,
    Other: 6,
};

const assetTypeLabels: Record<AssetTypeName, string> = {
    Computer: "Bilgisayar",
    Printer: "Yazıcı",
    Server: "Sunucu",
    NetworkDevice: "Ağ Cihazı",
    SoftwareSystem: "Yazılım Sistemi",
    Other: "Diğer",
};

export function AssetsPage() {
    const { user, token } = useAuth();

    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [departments, setDepartments] = useState<DepartmentDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [pageError, setPageError] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [isFormVisible, setIsFormVisible] = useState(false);
    const [editingAsset, setEditingAsset] = useState<AssetDto | null>(null);
    const [name, setName] = useState("");
    const [serialNumber, setSerialNumber] = useState("");
    const [assetType, setAssetType] = useState<AssetTypeValue>(1);
    const [departmentId, setDepartmentId] = useState("");
    const [location, setLocation] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [statusAssetId, setStatusAssetId] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadPageData() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setPageError(null);

                const [assetResult, departmentResult] =
                    await Promise.all([
                        getAssets(token),
                        user?.role === "Admin"
                            ? getDepartments(token)
                            : Promise.resolve([]),
                    ]);

                if (!cancelled) {
                    setAssets(assetResult);
                    setDepartments(departmentResult);
                }
            } catch (error) {
                if (!cancelled) {
                    setPageError(getErrorMessage(
                        error,
                        "Cihazlar yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadPageData();

        return () => {
            cancelled = true;
        };
    }, [token, user?.role]);

    const activeDepartments =
        departments.filter((department) => department.isActive);

    async function refreshAssets() {
        if (!token) {
            return;
        }

        const result = await getAssets(token);
        setAssets(result);
    }

    function resetForm() {
        setEditingAsset(null);
        setName("");
        setSerialNumber("");
        setAssetType(1);
        setDepartmentId("");
        setLocation("");
    }

    function startCreate() {
        resetForm();
        setActionError(null);
        setIsFormVisible(true);
    }

    function startEdit(asset: AssetDto) {
        setEditingAsset(asset);
        setName(asset.name);
        setSerialNumber(asset.serialNumber);
        setAssetType(assetTypeValues[asset.type]);
        setDepartmentId(
            activeDepartments.some(
                (department) => department.id === asset.departmentId
            )
                ? asset.departmentId
                : ""
        );
        setLocation(asset.location ?? "");
        setActionError(null);
        setIsFormVisible(true);
    }

    function closeForm() {
        setIsFormVisible(false);
        resetForm();
    }

    async function handleSubmit(
        event: SubmitEvent<HTMLFormElement>
    ) {
        event.preventDefault();

        if (!token || isSubmitting) {
            return;
        }

        const normalizedName = name.trim();
        const normalizedSerialNumber = serialNumber.trim();
        const normalizedLocation = location.trim();

        if (!normalizedName) {
            setActionError("Cihaz adı boş olamaz.");
            return;
        }

        if (!normalizedSerialNumber) {
            setActionError("Seri numarası boş olamaz.");
            return;
        }

        if (
            !departmentId ||
            !activeDepartments.some(
                (department) => department.id === departmentId
            )
        ) {
            setActionError("Aktif bir departman seçmelisiniz.");
            return;
        }

        if (!isAssetTypeValue(assetType)) {
            setActionError("Geçerli bir cihaz türü seçmelisiniz.");
            return;
        }

        try {
            setIsSubmitting(true);
            setActionError(null);

            const request = {
                name: normalizedName,
                serialNumber: normalizedSerialNumber,
                type: assetType,
                departmentId,
                location: normalizedLocation || null,
            };

            if (editingAsset) {
                await updateAsset(token, editingAsset.id, request);
            } else {
                await createAsset(token, request);
            }

            await refreshAssets();
            closeForm();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Cihaz kaydedilemedi."
            ));
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleStatusChange(asset: AssetDto) {
        if (!token || statusAssetId) {
            return;
        }

        try {
            setStatusAssetId(asset.id);
            setActionError(null);

            await changeAssetStatus(
                token,
                asset.id,
                { isActive: !asset.isActive }
            );

            await refreshAssets();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Cihaz durumu değiştirilemedi."
            ));
        } finally {
            setStatusAssetId(null);
        }
    }

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Cihazlar</h1>
                    <p className="page-description">
                        Bakım taleplerine bağlı donanım ve sistem envanterini
                        görüntüleyin ve yönetin.
                    </p>
                </div>

            {user?.role === "Admin" && !isFormVisible && (
                    <button
                        type="button"
                        className="button button-primary"
                        onClick={startCreate}
                    >
                    Yeni Cihaz
                </button>
            )}
            </header>

            {user?.role === "Admin" && isFormVisible && (
                <section className="card management-form-card">
                    <div className="card-header">
                        <div>
                            <h2>
                                {editingAsset ? "Cihazı Düzenle" : "Yeni Cihaz"}
                            </h2>
                            <p className="page-description">
                                {editingAsset
                                    ? "Cihazın temel envanter bilgilerini güncelleyin."
                                    : "Envantere yeni bir cihaz veya sistem ekleyin."}
                            </p>
                        </div>
                    </div>

                    {activeDepartments.length === 0 && (
                        <p className="error-state" role="alert">
                            Cihaz kaydetmek için aktif departman bulunamadı.
                        </p>
                    )}

                    <form onSubmit={handleSubmit}>
                        <div className="form-grid">
                        <div className="form-group">
                            <label htmlFor="asset-name">Ad</label>
                            <input
                                id="asset-name"
                                value={name}
                                maxLength={maxNameLength}
                                onChange={(event) =>
                                    setName(event.target.value)
                                }
                                disabled={isSubmitting}
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="asset-serial-number">
                                Seri Numarası
                            </label>
                            <input
                                id="asset-serial-number"
                                value={serialNumber}
                                maxLength={maxSerialNumberLength}
                                onChange={(event) =>
                                    setSerialNumber(event.target.value)
                                }
                                disabled={isSubmitting}
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="asset-type">Tür</label>
                            <select
                                id="asset-type"
                                value={assetType}
                                onChange={(event) =>
                                    setAssetType(
                                        Number(event.target.value) as
                                            AssetTypeValue
                                    )
                                }
                                disabled={isSubmitting}
                            >
                                {assetTypeOptions.map((option) => (
                                    <option
                                        key={option.value}
                                        value={option.value}
                                    >
                                        {option.label}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="form-group">
                            <label htmlFor="asset-department">
                                Departman
                            </label>
                            <select
                                id="asset-department"
                                value={departmentId}
                                onChange={(event) =>
                                    setDepartmentId(event.target.value)
                                }
                                disabled={
                                    isSubmitting ||
                                    activeDepartments.length === 0
                                }
                            >
                                <option value="">Departman seçin</option>
                                {activeDepartments.map((department) => (
                                    <option
                                        key={department.id}
                                        value={department.id}
                                    >
                                        {department.name}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="form-group form-group-full">
                            <label htmlFor="asset-location">Konum</label>
                            <input
                                id="asset-location"
                                value={location}
                                maxLength={maxLocationLength}
                                onChange={(event) =>
                                    setLocation(event.target.value)
                                }
                                disabled={isSubmitting}
                            />
                        </div>

                        </div>

                        <div className="form-actions">
                            <button
                                type="submit"
                                className="button button-primary"
                                disabled={
                                    isSubmitting ||
                                    activeDepartments.length === 0
                                }
                            >
                                {isSubmitting ? "Kaydediliyor..." : "Kaydet"}
                            </button>

                            <button
                                type="button"
                                className="button button-secondary"
                                onClick={closeForm}
                                disabled={isSubmitting}
                            >
                                Vazgeç
                            </button>
                        </div>
                    </form>
                </section>
            )}

            {actionError && (
                <p className="error-state" role="alert">{actionError}</p>
            )}
            {isLoading && (
                <p className="loading-state">Cihazlar yükleniyor...</p>
            )}
            {pageError && (
                <p className="error-state" role="alert">{pageError}</p>
            )}

            {!isLoading && !pageError && assets.length === 0 && (
                <p className="empty-state">Cihaz bulunamadı.</p>
            )}

            {!isLoading && !pageError && assets.length > 0 && (
                <div className="table-container">
                <table>
                    <thead>
                        <tr>
                            <th>Ad</th>
                            <th>Seri Numarası</th>
                            <th>Tür</th>
                            <th>Departman</th>
                            <th>Konum</th>
                            <th>Durum</th>
                            <th>İşlemler</th>
                        </tr>
                    </thead>

                    <tbody>
                        {assets.map((asset) => (
                            <tr key={asset.id}>
                                <td>{asset.name}</td>
                                <td>{asset.serialNumber}</td>
                                <td>{assetTypeLabels[asset.type]}</td>
                                <td>{asset.departmentName}</td>
                                <td>
                                    {asset.location ?? (
                                        <span className="muted-text">Belirtilmedi</span>
                                    )}
                                </td>
                                <td>
                                    <ActiveStatusBadge isActive={asset.isActive} />
                                </td>

                                <td>
                                    <div className="action-buttons">
                                        <Link
                                            to={`/assets/${asset.id}/history`}
                                            className="button button-secondary button-small"
                                        >
                                            Bakım Geçmişi
                                        </Link>
                                {user?.role === "Admin" && (
                                    <>
                                        <button
                                            type="button"
                                            className="button button-secondary button-small"
                                            onClick={() => startEdit(asset)}
                                            disabled={
                                                isSubmitting ||
                                                statusAssetId !== null
                                            }
                                        >
                                            Düzenle
                                        </button>

                                        <button
                                            type="button"
                                            className="button button-secondary button-small"
                                            onClick={() =>
                                                handleStatusChange(asset)
                                            }
                                            disabled={
                                                isSubmitting ||
                                                statusAssetId !== null
                                            }
                                        >
                                            {statusAssetId === asset.id
                                                ? "Değiştiriliyor..."
                                                : asset.isActive
                                                    ? "Pasif Yap"
                                                    : "Aktif Yap"}
                                        </button>
                                    </>
                                )}
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
                </div>
            )}
        </div>
    );
}

function isAssetTypeValue(value: number): value is AssetTypeValue {
    return value === 1 ||
        value === 2 ||
        value === 3 ||
        value === 4 ||
        value === 5 ||
        value === 6;
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
