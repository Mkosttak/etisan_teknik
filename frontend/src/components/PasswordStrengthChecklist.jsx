import React from 'react';
import { CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';

const PasswordStrengthChecklist = ({ password = '' }) => {
    // Password prop'u null veya undefined gelirse boş string olarak ele al
    const safePassword = password || '';

    const rules = [
        {
            id: 'length',
            label: 'En az 12 karakter',
            validator: (pwd) => pwd.length >= 12
        },
        {
            id: 'upper',
            label: 'En az 1 büyük harf',
            validator: (pwd) => /[A-Z]/.test(pwd)
        },
        {
            id: 'lower',
            label: 'En az 1 küçük harf',
            validator: (pwd) => /[a-z]/.test(pwd)
        },
        {
            id: 'number',
            label: 'En az 1 rakam',
            validator: (pwd) => /[0-9]/.test(pwd)
        },
        {
            id: 'special',
            label: 'En az 1 sembol (!@#$%^&*()_+-=[]{}|;:,.<>?)',
            validator: (pwd) => /[!@#$%^&*()_+\-=[\]{}|;:,.<>?]/.test(pwd)
        }
    ];

    return (
        <div style={{ marginTop: 8, padding: '8px 0' }}>
            {rules.map(rule => {
                const isValid = rule.validator(safePassword);
                return (
                    <div key={rule.id} style={{
                        color: isValid ? '#52c41a' : '#ff4d4f',
                        fontSize: '12px',
                        marginBottom: 4,
                        display: 'flex',
                        alignItems: 'center',
                        transition: 'all 0.3s'
                    }}>
                        <span style={{ marginRight: 8, display: 'flex' }}>
                            {isValid ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
                        </span>
                        {rule.label}
                    </div>
                );
            })}
        </div>
    );
};

export default PasswordStrengthChecklist;
