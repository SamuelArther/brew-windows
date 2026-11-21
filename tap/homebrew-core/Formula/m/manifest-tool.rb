class ManifestTool < Formula
  desc "Command-line tool to create and query container image manifest list/indexes"
  homepage "https://github.com/estesp/manifest-tool/"
  url "https://github.com/estesp/manifest-tool/archive/refs/tags/v2.2.3.tar.gz"
  sha256 "20f3136b61614bb72b4b95342644e78345a544091dc5ae660d0237ecebc102ff"
  license "Apache-2.0"
  head "https://github.com/estesp/manifest-tool.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2b20b037031d08c99f3a11a93bc1cae8878d7649b5c276947438ddb03aa093ad"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def install
    system "make", "all"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    package = "busybox:latest"
    stdout, stderr, = Open3.capture3(
      bin/"manifest-tool", "inspect",
      package
    )

    if stderr.lines.grep(/429 Too Many Requests/).first
      print "Can't test against docker hub\n"
      print stderr.lines.join("\n")
    else
      assert_match package, stdout.lines.grep(/^Name:/).first
      assert_match "sha", stdout.lines.grep(/Digest:/).first
      assert_match "Platform:", stdout.lines.grep(/Platform:/).first
      assert_match "OS:", stdout.lines.grep(/OS:\s*linux/).first
      assert_match "Arch:", stdout.lines.grep(/Arch:\s*amd64/).first
    end

    assert_match version.to_s, shell_output("#{bin}/manifest-tool --version")
  end
end
