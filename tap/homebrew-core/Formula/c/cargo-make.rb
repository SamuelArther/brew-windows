class CargoMake < Formula
  desc "Rust task runner and build tool"
  homepage "https://sagiegurari.github.io/cargo-make/"
  url "https://github.com/sagiegurari/cargo-make/releases/download/0.37.24/cargo-make-v0.37.24-x86_64-pc-windows-msvc.zip"
  sha256 "f7c757c7e147023ac86d80b134e5a59e0ed8ff032e39be3464cc4aad1e5e0f33"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sagiegurari/cargo-make/archive/refs/tags/0.37.24.tar.gz"
  end

  def install
    bin.install "cargo-make.exe"
    bin.install "makers.exe"
  end

  test do
    assert_path_exists bin/"cargo-make.exe"
  end
end
