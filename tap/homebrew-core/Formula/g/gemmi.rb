class Gemmi < Formula
  desc "Macromolecular crystallography library and utilities"
  homepage "https://project-gemmi.github.io/"
  url "https://github.com/project-gemmi/gemmi/archive/refs/tags/v0.7.5.tar.gz"
  sha256 "9e2a8a51e62c69bf43f62aadf527ca4312860de8a36c12a8747d3e8ae556f0b3"
  license "MPL-2.0"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/gemmi-0.7.5"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9cfe34597757ebeafd4a8573d2b4e59c6d81de93e07a7bd8a3408a5a6d1ed3ec"
  end

  depends_on "cmake" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
    pkgshare.install "tests"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gemmi --version")

    system bin/"gemmi", "validate", pkgshare/"tests/misc.cif"
  end
end
