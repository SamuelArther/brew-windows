class K6 < Formula
  desc "Modern load testing tool, using Go and JavaScript"
  homepage "https://k6.io"
  url "https://github.com/grafana/k6/releases/download/v2.3.0/k6-v2.3.0-windows-amd64.zip"
  sha256 "112276d495e5741c968e2bc09ea6196099c1275bd6db9ee0875d173c7148ce43"
  license "AGPL-3.0-or-later"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "k6.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/k6 version")
  end
end
