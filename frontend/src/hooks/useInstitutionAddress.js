import { useState, useCallback } from 'react'
import api from '../services/api'

// Hook for institution address auto-fill in forms
export const useInstitutionAddress = (form, cityCallback) => {
    const [addresses, setAddresses] = useState([])
    const [addressLoading, setAddressLoading] = useState(false)
    const [showAddressSelector, setShowAddressSelector] = useState(false)
    const [selectedAddressId, setSelectedAddressId] = useState(null)
    const [useDifferentAddress, setUseDifferentAddress] = useState(false)
    const [saveNewAddress, setSaveNewAddress] = useState(false)
    const [currentInstitution, setCurrentInstitution] = useState(null)

    // Fetch addresses when institution changes
    const handleInstitutionChange = useCallback(async (institutionName) => {
        // Reset states
        setAddresses([])
        setShowAddressSelector(false)
        setSelectedAddressId(null)
        setUseDifferentAddress(false)
        setSaveNewAddress(false)
        setCurrentInstitution(institutionName)

        // Clear address fields
        form.setFieldsValue({
            shippingCity: null,
            shippingDistrict: null,
            shippingAddressLine: null,
            contactPerson: null,
            contactPhone: null,
            contactEmail: null
        })
        if (cityCallback) cityCallback(null)

        // Don't fetch for "DİĞER" or empty
        if (!institutionName || institutionName === 'DİĞER') {
            return
        }

        try {
            setAddressLoading(true)
            const response = await api.get(`/institutions/by-name/${encodeURIComponent(institutionName)}/addresses`)
            const fetchedAddresses = response.data || []
            setAddresses(fetchedAddresses)

            if (fetchedAddresses.length === 1) {
                // Single address: auto-fill
                applyAddress(fetchedAddresses[0])
                setSelectedAddressId(fetchedAddresses[0].id)
            } else if (fetchedAddresses.length > 1) {
                // Multiple addresses: show selector
                setShowAddressSelector(true)
                // Auto-select default if exists
                const defaultAddr = fetchedAddresses.find(a => a.isDefault)
                if (defaultAddr) {
                    applyAddress(defaultAddr)
                    setSelectedAddressId(defaultAddr.id)
                }
            }
            // 0 addresses: fields remain empty, user enters manually
        } catch (error) {
            console.error('Failed to fetch addresses:', error)
        } finally {
            setAddressLoading(false)
        }
    }, [form, cityCallback])

    // Apply an address to the form
    const applyAddress = useCallback((address) => {
        form.setFieldsValue({
            shippingCity: address.city || null,
            shippingDistrict: address.district || null,
            shippingAddressLine: address.addressLine || null,
            contactPerson: address.contactPerson || null,
            contactPhone: address.phone || null,
            contactEmail: address.email || null
        })
        if (cityCallback && address.city) {
            cityCallback(address.city)
        }
    }, [form, cityCallback])

    // Handle address selection from dropdown
    const handleAddressSelect = useCallback((addressId) => {
        setSelectedAddressId(addressId)
        setUseDifferentAddress(false)

        if (addressId === 'different') {
            setUseDifferentAddress(true)
            // Clear fields for manual input
            form.setFieldsValue({
                shippingCity: null,
                shippingDistrict: null,
                shippingAddressLine: null,
                contactPerson: null,
                contactPhone: null,
                contactEmail: null
            })
            if (cityCallback) cityCallback(null)
        } else {
            const address = addresses.find(a => a.id === addressId)
            if (address) {
                applyAddress(address)
            }
        }
    }, [addresses, form, cityCallback, applyAddress])

    // Toggle "use different address"
    const handleUseDifferentToggle = useCallback((checked) => {
        setUseDifferentAddress(checked)
        if (checked) {
            form.setFieldsValue({
                shippingCity: null,
                shippingDistrict: null,
                shippingAddressLine: null,
                contactPerson: null,
                contactPhone: null,
                contactEmail: null
            })
            if (cityCallback) cityCallback(null)
        } else if (addresses.length > 0) {
            // Revert to first/default address
            const defaultAddr = addresses.find(a => a.isDefault) || addresses[0]
            applyAddress(defaultAddr)
            setSelectedAddressId(defaultAddr.id)
        }
    }, [addresses, form, cityCallback, applyAddress])

    // Save the entered address to database
    const saveAddressToDb = useCallback(async (label = 'Yeni Adres') => {
        if (!currentInstitution || currentInstitution === 'DİĞER') return

        const values = form.getFieldsValue()
        const addressData = {
            label: label,
            city: values.shippingCity,
            district: values.shippingDistrict,
            addressLine: values.shippingAddressLine,
            contactPerson: values.contactPerson,
            phone: values.contactPhone,
            email: values.contactEmail,
            isDefault: addresses.length === 0 // First address is default
        }

        try {
            await api.post(`/institutions/by-name/${encodeURIComponent(currentInstitution)}/addresses`, addressData)
            return true
        } catch (error) {
            console.error('Failed to save address:', error)
            return false
        }
    }, [currentInstitution, addresses, form])

    return {
        addresses,
        addressLoading,
        showAddressSelector,
        selectedAddressId,
        useDifferentAddress,
        saveNewAddress,
        currentInstitution,
        handleInstitutionChange,
        handleAddressSelect,
        handleUseDifferentToggle,
        setSaveNewAddress,
        saveAddressToDb,
        isAddressReadonly: addresses.length > 0 && !useDifferentAddress
    }
}

export default useInstitutionAddress
