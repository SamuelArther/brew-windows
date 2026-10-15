class CargoSort < Formula
  desc "Tool to check that your Cargo.toml dependencies are sorted alphabetically"
  homepage "https://github.com/devinr528/cargo-sort"
  url "https://github.com/DevinR528/cargo-sort/releases/download/v2.1.4/cargo-sort-x86_64-pc-windows-msvc.zip"
  sha256 "0c59ef2433e523e060909cdfa678969eaa81a44fdc4313cb5a1a9592e8ded2b3"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/DevinR528/cargo-sort/archive/refs/tags/v2.1.4.tar.gz"
  end

  def install
    bin.install "cargo-sort.exe"
  end

  test do
    system bin/"cargo-sort", "--version"
  end
end
