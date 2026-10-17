class Pyoxidizer < Formula
  desc "Modern Python application packaging and distribution tool"
  homepage "https://github.com/indygreg/PyOxidizer"
  url "https://github.com/indygreg/PyOxidizer/archive/refs/tags/pyoxidizer/0.24.0.tar.gz"
  sha256 "d52a2727a18a3414d7ec920b8523ef4d19cb2e6db3f1276b65a83e5dd4ae8d24"
  license "MPL-2.0"
  head "https://github.com/indygreg/PyOxidizer.git", branch: "main"

  livecheck do
    url :stable
    regex(%r{^pyoxidizer/v?(\d+(?:\.\d+)+)$}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "a1955513a47197ed9b00a684a7c86fe20f2e727dee9bf490b70eb3803502a378"
  end

  depends_on "rust" => :build

  def install
    # The old libgit2-sys in its build script calls advapi32 without linking it, which Rust's std no longer does for it.
    ENV.append "RUSTFLAGS", "-l advapi32" if OS.windows?
    system "cargo", "install", *std_cargo_args(path: "pyoxidizer")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pyoxidizer --version")

    # FIXME: restore brew `rust` usage if support is added for newer Rust
    # system bin/"pyoxidizer", "init-rust-project", "--system-rust", "hello_world"
    system bin/"pyoxidizer", "init-rust-project", "hello_world"
    assert_path_exists testpath/"hello_world/Cargo.toml"

    # Intel macOS runners are slow enough that extra time from fetching Rust causes timeout
    return if OS.mac? && Hardware::CPU.intel?

    cd "hello_world" do
      if Hardware::CPU.arm? && OS.mac? && MacOS.version < :ventura
        # Use Python 3.8 to work around:
        # https://github.com/Homebrew/homebrew-core/pull/136910#issuecomment-1704568838
        inreplace "pyoxidizer.bzl",
                  "dist = default_python_distribution()",
                  "dist = default_python_distribution(python_version='3.8')"
      end
      system bin/"pyoxidizer", "build" # FIXME: , "--system-rust"
    end
  end
end
