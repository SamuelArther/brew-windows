class CargoRelease < Formula
  desc "Cargo subcommand `release`: everything about releasing a rust crate"
  homepage "https://github.com/crate-ci/cargo-release"
  url "https://github.com/crate-ci/cargo-release/releases/download/v1.1.6/cargo-release-v1.1.6-x86_64-pc-windows-msvc.zip"
  sha256 "511ac6eb265f137cf78c74f6488bcdad750e669b5dd63324cc5582ea5a59ab5e"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/crate-ci/cargo-release/archive/refs/tags/v1.1.6.tar.gz"
  end

  def install
    bin.install "cargo-release.exe"
  end

  test do
    system bin/"cargo-release", "--help"
  end
end
