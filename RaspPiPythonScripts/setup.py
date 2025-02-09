from setuptools import setup, find_packages

setup(
    name='SmartTrafficControlSystemClient',
    version='0.1.0',
    packages=find_packages(),
    install_requires=[
        'required-package1',
        'required-package2',
    ],
    entry_points={
        'console_scripts': [
            'my-app=SmartTrafficControlSystemClient.core:main',  # Example CLI entry point
        ],
    },
)

# run this command in the terminal to install the package:
# python setup.py sdist
# pip3 install dist/SmartTrafficControlSystemClient-0.1.0.tar.gz
