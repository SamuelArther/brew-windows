class Rustpython < Formula
  desc "Python Interpreter written in Rust"
  homepage "https://rustpython.github.io"
  url "https://github.com/RustPython/RustPython/archive/refs/tags/0.6.0.tar.gz"
  sha256 "bf290cf7a70f813758819d895868b2b49b9edea1f10de3524df2cae1f2d58a4d"
  license "MIT"
  head "https://github.com/RustPython/RustPython.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "ab64de42ef1b65ae833abe3a05032b53624c0a2d6567c8c5b31721579ab002c2"
  end

  depends_on "rust" => :build

  uses_from_macos "libffi"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", "--features=freeze-stdlib", *std_cargo_args
  end

  test do
    system bin/"rustpython", "-c", "print('Hello, RustPython!')"
    system bin/"rustpython", "-c", "import sys"
    system bin/"rustpython", "-m", "venv", "--without-pip", testpath/".venv"
  end
end
