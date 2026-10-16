class CargoUdeps < Formula
  desc "Find unused dependencies in Cargo.toml"
  homepage "https://github.com/est31/cargo-udeps"
  url "https://github.com/est31/cargo-udeps/releases/download/v0.1.61/cargo-udeps-v0.1.61-x86_64-pc-windows-msvc.zip"
  sha256 "6920b6f493f0c13a28e32522ccb7efffcc57c8849d887366b19c599dc2238cc0"
  license any_of: ["Apache-2.0", "MIT"]
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/est31/cargo-udeps/archive/refs/tags/v0.1.61.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/cargo-udeps.exe", "cargo-udeps"
  end

  test do
    system bin/"cargo-udeps", "--help"
  end
end
