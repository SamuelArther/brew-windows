class ElanInit < Formula
  desc "Lean Theorem Prover installer and version manager"
  homepage "https://github.com/leanprover/elan"
  url "https://github.com/leanprover/elan/releases/download/v4.2.4/elan-x86_64-pc-windows-msvc.zip"
  sha256 "fad2e980a191c15884cc1d80d170ffc5fa84f3774541020145b66d1a644c6111"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/leanprover/elan/archive/refs/tags/v4.2.4.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "elan-init.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/elan-init --version 2>&1")
  end
end
