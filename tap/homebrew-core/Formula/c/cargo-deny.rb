class CargoDeny < Formula
  desc "Cargo plugin for linting your dependencies"
  homepage "https://github.com/EmbarkStudios/cargo-deny"
  url "https://github.com/EmbarkStudios/cargo-deny/releases/download/0.20.2/cargo-deny-0.20.2-x86_64-pc-windows-msvc.tar.gz"
  sha256 "975a22143262fd27476d19ee00c7af67978426e40e1dee94eed6bbade1cf87dc"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/EmbarkStudios/cargo-deny/archive/refs/tags/0.20.2.tar.gz"
  end

  def install
    bin.install "cargo-deny.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cargo-deny --version 2>&1")
  end
end
