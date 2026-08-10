export interface AuditLogDto {
    id: string;
    performedByUserId: string;
    performedByUserFullName: string;
    action: string;
    entityName: string;
    entityId: string;
    oldValues: string | null;
    newValues: string | null;
    createdAt: string;
}

export interface AuditLogListQuery {
    pageNumber: number;
    pageSize: number;
    performedByUserId?: string;
    action?: string;
    entityName?: string;
    entityId?: string;
    startDate?: string;
    endDate?: string;
}
