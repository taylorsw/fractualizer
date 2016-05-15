struct Complex
{
	float real;
	float imaginary;
};

Complex add(Complex c1, Complex c2)
{
	Complex result;
	result.real = c1.real + c2.real;
	result.imaginary = c1.imaginary + c2.imaginary;
	return result;
}

Complex multiply(Complex c1, Complex c2)
{
	Complex result;
	result.real = c1.real * c2.real - c1.imaginary * c2.imaginary;
	result.imaginary = c1.real * c2.imaginary + c1.imaginary * c2.real;
	return result;
}

Complex square(Complex c)
{
	return multiply(c, c);
}

Complex newComplex(float real, float imaginary)
{
	Complex result;
	result.real = real;
	result.imaginary = imaginary;
	return result;
}

float magnitude(Complex c)
{
	return sqrt(c.real * c.real + c.imaginary * c.imaginary);
}

float4 main(float4 position : SV_POSITION) : SV_TARGET
{
	Complex z = newComplex(0, 0);
Complex c = newComplex((position.x - 600) / 200, (position.y - 350) / 200);
for (uint i = 0; i < 100; i++) {
	z = add(square(z), c);
}
float4 result;
if (magnitude(z) < 2) {
	result = float4(1, 0, 0, 1);
}
else {
	result = float4(0, 0, 0, 1);
}
return result;
}