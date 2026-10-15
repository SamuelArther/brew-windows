class CargoLlvmCov < Formula
  desc "Cargo subcommand to easily use LLVM source-based code coverage"
  homepage "https://github.com/taiki-e/cargo-llvm-cov"
  url "https://github.com/taiki-e/cargo-llvm-cov/releases/download/v0.9.1/cargo-llvm-cov-x86_64-pc-windows-msvc.tar.gz"
  sha256 "15eefd5b90fcdd4006b727c6e710d70a61aff509fcd7bb37f9c2a35c4f024948"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://static.crates.io/crates/cargo-llvm-cov/cargo-llvm-cov-0.9.1.crate"
  end

  def install
    bin.install "cargo-llvm-cov.exe"
  end

  test do
    assert_path_exists bin/"cargo-llvm-cov.exe"
  end
end
