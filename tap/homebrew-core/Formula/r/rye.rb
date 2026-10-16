class Rye < Formula
  desc "Package Management Solution for Python"
  homepage "https://rye.astral.sh/"
  url "https://github.com/astral-sh/rye/archive/refs/tags/0.44.0.tar.gz"
  sha256 "6ef86ccba82b59edfc4f6deba39be6394e7866fe2250596b96124c20327f0581"
  license "MIT"

  # https://github.com/astral-sh/rye/commit/62ec9edbe471958a05a70418b19f3acd54f0484d
  deprecate! date: "2025-08-18", because: :unmaintained, replacement_formula: "uv"
  disable! date: "2026-08-18", because: :unmaintained, replacement_formula: "uv"

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  uses_from_macos "bzip2"

  on_linux do
    depends_on "openssl@3"
    depends_on "zlib-ng-compat"
  end

  conflicts_with "ryelang", because: "both install `rye` binaries"

  def install
    system "cargo", "install", *std_cargo_args(path: "rye")
    generate_completions_from_executable(bin/"rye", "self", "completion", "-s")
  end

  test do
    (testpath/"pyproject.toml").write <<~TOML
      [project]
      name = "testproj"
      requires-python = ">=3.9"
      version = "1.0"
      license = {text = "MIT"}

    TOML
    system bin/"rye", "add", "requests==2.24.0"
    system bin/"rye", "sync"
    assert_match "requests==2.24.0", (testpath/"pyproject.toml").read
    output = shell_output("#{bin}/rye run python -c 'import requests;print(requests.__version__)'")
    assert_equal "2.24.0", output.strip
  end
end
