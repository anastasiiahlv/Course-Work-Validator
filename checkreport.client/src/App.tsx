import React from 'react';
import FileUpload from './FileUpload';
import './App.css';

const App: React.FC = () => {
    return (
        <div className="container">
            <h1>Перевірка курсових робіт</h1>
            <p>Завантажте документ у форматі .docx для перевірки на відповідність вимогам.</p>
            <FileUpload />
        </div>
    );
};

export default App;

