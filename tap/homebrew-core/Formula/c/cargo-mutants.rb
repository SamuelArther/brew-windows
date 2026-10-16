class CargoMutants < Formula
  desc "Inject bugs and see if your tests catch them"
  homepage "https://mutants.rs/"
  url "https://github.com/sourcefrog/cargo-mutants/releases/download/v27.1.0/cargo-mutants-x86_64-pc-windows-msvc.zip"
  sha256 "2a2f00e47d4b458262a41501b0820aa26015fd35779903d2c8b30b2993f36791"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sourcefrog/cargo-mutants/archive/refs/tags/v27.1.0.tar.gz"
  end

  def install
    bin.install "cargo-mutants.exe"
  end

  test do
    system bin/"cargo-mutants", "--help"
  end
end
