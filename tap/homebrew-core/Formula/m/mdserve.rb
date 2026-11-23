class Mdserve < Formula
  desc "Fast markdown preview server with live reload and theme support"
  homepage "https://github.com/jfernandez/mdserve"
  url "https://github.com/jfernandez/mdserve/archive/refs/tags/v1.1.0.tar.gz"
  sha256 "a13490d0b63af960cb74507e2cd684b4ecc0f59325e1946f04e82595e2641aa8"
  license "MIT"
  head "https://github.com/jfernandez/mdserve.git", branch: "main"

  deprecate! date: "2026-11-19", because: :repo_archived
  disable! date: "2027-05-19", because: :repo_archived

  bottle do
    sha256 cellar: :any, x86_64_windows: "9157c80b09bb57c950b176288c7ca354fa23d387479e7ba07497558da7fce0df"
  end

  depends_on "rust" => :build

  # The test runs a local HTTP server.
  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mdserve --version")

    (testpath/"test.md").write("# Test\n\nThis is a test markdown file.")

    port = free_port
    with_test_process(bin/"mdserve", "--port", port.to_s, "test.md") do
      sleep 1

      output = shell_output("curl -s http://localhost:#{port}")
      assert_match "This is a test markdown file", output
    end
  end
end
