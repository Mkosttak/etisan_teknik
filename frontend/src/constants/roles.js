/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: roles.js
 * KONUM: frontend/src/constants/
 * TİP: Frontend - Constants
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Uygulamadaki kullanıcı rollerini tanımlar.
 * Backend AppRoles.cs ile TAM UYUMLU olmalıdır!
 * 
 * KULLANIM:
 * import { ROLES, PRIVILEGED_ROLES } from '@/constants/roles'
 * if (user.role === ROLES.ADMIN) { ... }
 * if (PRIVILEGED_ROLES.includes(user.role)) { ... }
 * 
 * ROL AÇIKLAMALARI:
 * - ADMIN: Tüm yetkilere sahip sistem yöneticisi
 * - PROJE: Proje yetkilerine sahip kullanıcı
 * - TEKNIK: Teknik servis personeli
 * - SAHA_ELEMANI: Saha personeli (sadece requests, faults, service-forms)
 * - KULLANICI: Kullanıcı
 * 
 * KRITIK:
 * Bu değerler backend TeknikServisAPI/Constants/AppConstants.cs ile
 * TAMAMEN AYNI olmalıdır. Değişiklik yapılırken her iki taraf da güncellenmelidir!
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

// Backend AppRoles ile TAM UYUMLU
export const ROLES = {
    ADMIN: 'Admin',
    PROJE: 'Proje',
    TEKNIK: 'Teknik',
    SAHA_ELEMANI: 'SahaElemani',
    KULLANICI: 'Kullanici'
};

// İzinli roller - Talep sahipliği kontrolünden muaf
export const PRIVILEGED_ROLES = [ROLES.ADMIN, ROLES.PROJE];

// Tüm geçerli roller
export const ALL_ROLES = [
    ROLES.ADMIN,
    ROLES.PROJE,
    ROLES.TEKNIK,
    ROLES.SAHA_ELEMANI,
    ROLES.KULLANICI
];
