class Certgraph < Formula
  desc "Crawl the graph of certificate Alternate Names"
  homepage "https://lanrat.github.io/certgraph/"
  url "https://github.com/lanrat/certgraph/releases/download/v0.1.3/certgraph_windows_amd64.zip"
  sha256 "274a308efe956b16fc310ff42536dc02b0853eb6c91cf0aded4c062875236485"
  license "GPL-2.0-or-later"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lanrat/certgraph/archive/refs/tags/v0.1.3.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "certgraph.exe"
  end

  test do
    output = shell_output("#{bin}/certgraph github.io")
    assert_match "githubusercontent.com", output
    assert_match "pages.github.com", output

    assert_match version.to_s, shell_output("#{bin}/certgraph --version")
  end
end
