import React, { useState } from 'react';

const FileUpload: React.FC = () => {
    const [file, setFile] = useState<File | null>(null);
    const [errors, setErrors] = useState<{ [key: string]: string[] }>({});
    const [message, setMessage] = useState<string>("");
    const [loading, setLoading] = useState<boolean>(false);

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const selectedFile = event.target.files?.[0];
        if (selectedFile) {
            if (selectedFile.type !== "application/pdf") {
                setErrors({ general: ["Невірний формат файлу. Завантажуйте тільки PDF."] });
                setFile(null);
                return;
            }
            setFile(selectedFile);
            setErrors({});
            setMessage("");
        }
    };

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (!file) {
            setErrors({ general: ["Будь ласка, виберіть файл!"] });
            return;
        }

        const formData = new FormData();
        formData.append('file', file);
        setLoading(true);

        try {
            const response = await fetch('https://localhost:7270/api/validate', {
                method: 'POST',
                body: formData,
                headers: { "Accept": "application/json" }
            });

            const text = await response.text();
            console.log("Відповідь сервера:", text);

            try {
                const result = JSON.parse(text);
                if (response.ok) {
                    setMessage(result.message);
                    setErrors({});
                } else {
                    setErrors(result.errors || { general: ["Невідома помилка"] });
                    setMessage("");
                }
            } catch (error) {
                setErrors({ general: ["Помилка обробки відповіді від сервера."] });
                setMessage("");
            }

        } catch (error) {
            setErrors({ general: ["Помилка з'єднання з сервером."] });
            setMessage("");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="container">
            <h1>Перевірка курсової роботи</h1>
            <p>Завантажте документ у форматі .pdf для перевірки на відповідність вимогам.</p>
            <form onSubmit={handleSubmit} className="file-upload">
                <input type="file" onChange={handleFileChange} />
                <button type="submit" disabled={loading}>
                    {loading ? "Перевіряється..." : "Перевірити"}
                </button>
            </form>

            {message && <p className="message" style={{ color: "green" }}>{message}</p>}

            {Object.keys(errors).length > 0 && (
                <div className="error-container">
                    {errors.general && (
                        <ul className="error-list">
                            {errors.general.map((error, index) => (
                                <li key={index}>{error}</li>
                            ))}
                        </ul>
                    )}

                    {Object.entries(errors)
                        .filter(([key]) => key !== "general")
                        .map(([section, sectionErrors]) => (
                            <div key={section}>
                                <strong>{section}</strong>
                                <ul className="error-list">
                                    {sectionErrors.map((error, index) => (
                                        <li key={index}>{error}</li>
                                    ))}
                                </ul>
                            </div>
                        ))}
                </div>
            )}
        </div>
    );
};

export default FileUpload;
