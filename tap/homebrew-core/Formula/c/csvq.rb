class Csvq < Formula
  desc "SQL-like query language for csv"
  homepage "https://mithrandie.github.io/csvq"
  url "https://github.com/mithrandie/csvq/releases/download/v1.18.1/csvq-v1.18.1-windows-amd64.tar.gz"
  sha256 "a63a5753aa9914abb34849b76d2075755ffde2ca194215b34c0799423a3b67a4"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mithrandie/csvq/archive/refs/tags/v1.18.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "csvq.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/csvq --version 2>&1")
  end
end
