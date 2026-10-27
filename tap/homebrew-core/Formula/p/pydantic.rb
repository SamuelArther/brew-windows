class Pydantic < Formula
  include Language::Python::Virtualenv

  desc "Data validation using Python type hints"
  homepage "https://pydantic.dev/docs/validation"
  url "https://files.pythonhosted.org/packages/7c/0b/8e10b2e693af8ec54346a14caf36221334775975a747c9ceaa3f8371d96d/pydantic-2.14.1.tar.gz"
  sha256 "94f478203dd03404682a1ada216965651dd74b1d2d5ffd62e00e0837caab5c26"
  license "MIT"
  version_scheme 1
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "5216c51cc38e8e45d9b64c9bacfa923914664a8c44bcfc39eb55428135623b7e"
  end

  depends_on "maturin" => :build
  depends_on "python@3.14" => [:build, :test]
  depends_on "python@3.15" => [:build, :test]
  depends_on "rust" => :build

  def pythons
    deps.map(&:to_formula)
        .select { |f| f.name.start_with?("python@") }
        .map { |f| f.opt_libexec/"bin/python" }
  end

  resource "annotated-types" do
    url "https://files.pythonhosted.org/packages/5f/56/a8120250d128bed162cd73c76d45f6ef9991f3e068f62a8ee060afa3104a/annotated_types-0.8.0.tar.gz"
    sha256 "13b2beaad985e05e2d6407ee4c4f35590b11f8d693a258a561055cac8f64cab7"
  end

  resource "pydantic-core" do
    url "https://files.pythonhosted.org/packages/a6/24/af4ec4be49fbc810f35b0bdcacc3d433b56bbfa469fd3bfd4ae116cd9bf1/pydantic_core-2.50.1.tar.gz"
    sha256 "e50d7b94baac6c7d09927fa5ca5800a0c7ee5015c7fcff65beb3a1931b5a6e09"
  end

  resource "typing-extensions" do
    url "https://files.pythonhosted.org/packages/f6/cc/6253133b5bb138fc3306cebfbda2c520f545d36b5be2c7255cc528bb45d6/typing_extensions-4.16.0.tar.gz"
    sha256 "dc983d19a509c94dba722ee6abd33940f7c05a89e243c47e907eb4db6f1a43e5"
  end

  resource "typing-inspection" do
    url "https://files.pythonhosted.org/packages/a3/26/b09b8010994eccc3c09092e6b34058f36a460eea2d4c3e8b910c695975a0/typing_inspection-0.4.4.tar.gz"
    sha256 "547274fa6b0a561ccf549cc9524b999a578e737d015d8709d021f9d0d13bea47"
  end

  allow_network_access! :build

  def install
    pythons.each do |python3|
      resources.each do |r|
        r.stage do
          build_isolation = r.name != "pydantic-core"
          system python3, "-m", "pip", "install", *std_pip_args(build_isolation:), "."
        end
      end

      system python3, "-m", "pip", "install", *std_pip_args(build_isolation: true), "."
    end
  end

  test do
    pythons.each do |python3|
      system python3, "-c", <<~PYTHON
        from pydantic import BaseModel, ValidationError

        class Model(BaseModel):
            value: int

        assert Model(value="42").model_dump() == {"value": 42}
        try:
            Model(value="invalid")
        except ValidationError as error:
            assert error.errors()[0]["type"] == "int_parsing"
        else:
            raise AssertionError("Invalid input was accepted")
      PYTHON
    end
  end
end
