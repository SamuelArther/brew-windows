class Odbc2parquet < Formula
  desc "CLI to query an ODBC data source and write the result into a Parquet file"
  homepage "https://github.com/pacman82/odbc2parquet"
  url "https://github.com/pacman82/odbc2parquet/releases/download/v11.0.1/odbc2parquet-win-x86_64.zip"
  sha256 "8356b19f0cdf3fa9ce9333b5d1a675dfd01a1149ecf77b84cbf2b3553e061811"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pacman82/odbc2parquet/archive/refs/tags/v11.0.1.tar.gz"
  end

  def install
    bin.install "odbc2parquet.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/odbc2parquet --version")

    system bin/"odbc2parquet", "list-data-sources"
    system bin/"odbc2parquet", "list-drivers"
  end
end
