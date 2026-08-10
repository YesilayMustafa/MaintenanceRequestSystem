import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";

import { getDepartments } from "../api/departmentsApi";
import { ApiError } from "../api/httpClient";
import {
    changeUserRole,
    changeUserStatus,
    createUser,
    getUsers,
    updateUser,
} from "../api/usersApi";
import { useAuth } from "../auth/useAuth";
import { AppNavigation } from "../components/AppNavigation";

import type { UserRole } from "../types/auth";
import type { DepartmentDto } from "../types/departments";
import type {
    UserDto,
    UserRoleValue,
} from "../types/users";

const roleOptions: Array<{
    label: string;
    name: UserRole;
    value: UserRoleValue;
}> = [
    { label: "Çalışan", name: "Employee", value: 1 },
    { label: "Teknik Personel", name: "Technician", value: 2 },
    { label: "Admin", name: "Admin", value: 3 },
];

const roleValues: Record<UserRole, UserRoleValue> = {
    Employee: 1,
    Technician: 2,
    Admin: 3,
};

export function UsersPage() {
    const { token } = useAuth();

    const [users, setUsers] = useState<UserDto[]>([]);
    const [departments, setDepartments] = useState<DepartmentDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [pageError, setPageError] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [isFormVisible, setIsFormVisible] = useState(false);
    const [editingUser, setEditingUser] = useState<UserDto | null>(null);
    const [fullName, setFullName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [role, setRole] = useState<UserRoleValue>(1);
    const [departmentId, setDepartmentId] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [activeActionUserId, setActiveActionUserId] =
        useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadPageData() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setPageError(null);

                const [userResult, departmentResult] =
                    await Promise.all([
                        getUsers(token),
                        getDepartments(token),
                    ]);

                if (!cancelled) {
                    setUsers(userResult);
                    setDepartments(departmentResult);
                }
            } catch (error) {
                if (!cancelled) {
                    setPageError(getErrorMessage(
                        error,
                        "Kullanıcılar yüklenemedi."
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
    }, [token]);

    const activeDepartments = departments.filter(
        (department) => department.isActive
    );

    async function refreshUsers() {
        if (!token) {
            return;
        }

        setUsers(await getUsers(token));
    }

    function resetForm() {
        setEditingUser(null);
        setFullName("");
        setEmail("");
        setPassword("");
        setRole(1);
        setDepartmentId("");
    }

    function startCreate() {
        resetForm();
        setActionError(null);
        setIsFormVisible(true);
    }

    function startEdit(user: UserDto) {
        setEditingUser(user);
        setFullName(user.fullName);
        setEmail(user.email);
        setPassword("");
        setRole(roleValues[user.role]);
        setDepartmentId(
            activeDepartments.some(
                (department) => department.id === user.departmentId
            )
                ? user.departmentId
                : ""
        );
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

        const normalizedFullName = fullName.trim();
        const normalizedEmail = email.trim();

        if (!normalizedFullName || !normalizedEmail) {
            setActionError("Ad soyad ve e-posta alanları zorunludur.");
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

        if (!editingUser && password.length < 8) {
            setActionError("Parola en az 8 karakter olmalıdır.");
            return;
        }

        try {
            setIsSubmitting(true);
            setActionError(null);

            if (editingUser) {
                await updateUser(token, editingUser.id, {
                    fullName: normalizedFullName,
                    email: normalizedEmail,
                    departmentId,
                });
            } else {
                await createUser(token, {
                    fullName: normalizedFullName,
                    email: normalizedEmail,
                    password,
                    role,
                    departmentId,
                });
            }

            await refreshUsers();
            closeForm();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Kullanıcı kaydedilemedi."
            ));
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleStatusChange(user: UserDto) {
        if (!token || activeActionUserId) {
            return;
        }

        try {
            setActiveActionUserId(user.id);
            setActionError(null);
            await changeUserStatus(token, user.id, {
                isActive: !user.isActive,
            });
            await refreshUsers();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Kullanıcı durumu değiştirilemedi."
            ));
        } finally {
            setActiveActionUserId(null);
        }
    }

    async function handleRoleChange(
        user: UserDto,
        nextRole: UserRoleValue
    ) {
        if (
            !token ||
            activeActionUserId ||
            roleValues[user.role] === nextRole
        ) {
            return;
        }

        try {
            setActiveActionUserId(user.id);
            setActionError(null);
            await changeUserRole(token, user.id, { role: nextRole });
            await refreshUsers();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Kullanıcı rolü değiştirilemedi."
            ));
        } finally {
            setActiveActionUserId(null);
        }
    }

    return (
        <main>
            <AppNavigation />

            <h1>Kullanıcılar</h1>

            {!isFormVisible && (
                <button type="button" onClick={startCreate}>
                    Yeni Kullanıcı
                </button>
            )}

            {isFormVisible && (
                <section>
                    <h2>
                        {editingUser
                            ? "Kullanıcı Düzenle"
                            : "Yeni Kullanıcı"}
                    </h2>

                    {activeDepartments.length === 0 && (
                        <p role="alert">
                            Kullanıcı kaydetmek için aktif departman bulunamadı.
                        </p>
                    )}

                    <form onSubmit={handleSubmit}>
                        <div>
                            <label htmlFor="user-full-name">Ad Soyad</label>
                            <input
                                id="user-full-name"
                                value={fullName}
                                maxLength={150}
                                onChange={(event) =>
                                    setFullName(event.target.value)
                                }
                                disabled={isSubmitting}
                            />
                        </div>

                        <div>
                            <label htmlFor="user-email">E-posta</label>
                            <input
                                id="user-email"
                                type="email"
                                value={email}
                                maxLength={255}
                                onChange={(event) =>
                                    setEmail(event.target.value)
                                }
                                disabled={isSubmitting}
                            />
                        </div>

                        {!editingUser && (
                            <div>
                                <label htmlFor="user-password">Parola</label>
                                <input
                                    id="user-password"
                                    type="password"
                                    value={password}
                                    minLength={8}
                                    maxLength={128}
                                    autoComplete="new-password"
                                    onChange={(event) =>
                                        setPassword(event.target.value)
                                    }
                                    disabled={isSubmitting}
                                />
                            </div>
                        )}

                        {!editingUser && (
                            <div>
                                <label htmlFor="user-role">Rol</label>
                                <select
                                    id="user-role"
                                    value={role}
                                    onChange={(event) =>
                                        setRole(
                                            Number(event.target.value) as
                                                UserRoleValue
                                        )
                                    }
                                    disabled={isSubmitting}
                                >
                                    {roleOptions.map((option) => (
                                        <option
                                            key={option.value}
                                            value={option.value}
                                        >
                                            {option.label}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        )}

                        <div>
                            <label htmlFor="user-department">Departman</label>
                            <select
                                id="user-department"
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

                        <button
                            type="submit"
                            disabled={
                                isSubmitting ||
                                activeDepartments.length === 0
                            }
                        >
                            {isSubmitting ? "Kaydediliyor..." : "Kaydet"}
                        </button>

                        <button
                            type="button"
                            onClick={closeForm}
                            disabled={isSubmitting}
                        >
                            Vazgeç
                        </button>
                    </form>
                </section>
            )}

            {actionError && <p role="alert">{actionError}</p>}
            {isLoading && <p>Kullanıcılar yükleniyor...</p>}
            {pageError && <p role="alert">{pageError}</p>}

            {!isLoading && !pageError && users.length === 0 && (
                <p>Kullanıcı bulunamadı.</p>
            )}

            {!isLoading && !pageError && users.length > 0 && (
                <table>
                    <thead>
                        <tr>
                            <th>Ad Soyad</th>
                            <th>E-posta</th>
                            <th>Rol</th>
                            <th>Departman</th>
                            <th>Durum</th>
                            <th>Oluşturulma</th>
                            <th>İşlemler</th>
                        </tr>
                    </thead>

                    <tbody>
                        {users.map((user) => (
                            <tr key={user.id}>
                                <td>{user.fullName}</td>
                                <td>{user.email}</td>
                                <td>
                                    <select
                                        aria-label={`${user.fullName} rolü`}
                                        value={roleValues[user.role]}
                                        onChange={(event) =>
                                            handleRoleChange(
                                                user,
                                                Number(event.target.value) as
                                                    UserRoleValue
                                            )
                                        }
                                        disabled={activeActionUserId !== null}
                                    >
                                        {roleOptions.map((option) => (
                                            <option
                                                key={option.value}
                                                value={option.value}
                                            >
                                                {option.label}
                                            </option>
                                        ))}
                                    </select>
                                </td>
                                <td>{user.departmentName}</td>
                                <td>{user.isActive ? "Aktif" : "Pasif"}</td>
                                <td>
                                    {new Date(user.createdAt)
                                        .toLocaleString("tr-TR")}
                                </td>
                                <td>
                                    <button
                                        type="button"
                                        onClick={() => startEdit(user)}
                                        disabled={activeActionUserId !== null}
                                    >
                                        Düzenle
                                    </button>

                                    <button
                                        type="button"
                                        onClick={() => handleStatusChange(user)}
                                        disabled={activeActionUserId !== null}
                                    >
                                        {activeActionUserId === user.id
                                            ? "Değiştiriliyor..."
                                            : user.isActive
                                                ? "Pasif Yap"
                                                : "Aktif Yap"}
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </main>
    );
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
