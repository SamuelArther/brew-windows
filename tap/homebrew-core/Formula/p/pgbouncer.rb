class Pgbouncer < Formula
  desc "Lightweight connection pooler for PostgreSQL"
  homepage "https://www.pgbouncer.org/"
  url "https://github.com/pgbouncer/pgbouncer/releases/download/pgbouncer_1_26_1/pgbouncer-1.26.1-windows-x86_64.zip"
  version "1.26.1"
  sha256 "5d3fa0bc9bd40f8845655f50a9bafd315db9e9ac51ee1a511393e9f60828b57b"
  license "ISC"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://www.pgbouncer.org/downloads/"
    regex(/href=.*?pgbouncer[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    windows_exe "libexec/pgbouncer.exe", "pgbouncer"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pgbouncer -V")
  end
end
