class Mycorrhiza < Formula
  desc "Lightweight wiki engine with hierarchy support"
  homepage "https://mycorrhiza.wiki"
  url "https://github.com/bouncepaw/mycorrhiza/archive/refs/tags/v1.16.0.tar.gz"
  sha256 "1724c0439a6d0174841cb777a13ab82b189c3e3a855f858cd24a62a8620a1e2f"
  license "AGPL-3.0-only"
  head "https://github.com/bouncepaw/mycorrhiza.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e9f1b6288046981db6e6c32d9cdfb8648b90de1988f424c3f82612e35348c9f3"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "make", "PREFIX=#{prefix}"
    system "make", "install", "PREFIX=#{prefix}"
  end

  service do
    run [opt_bin/"mycorrhiza", var/"lib/mycorrhiza"]
    keep_alive true
    log_path var/"log/mycorrhiza.log"
    error_log_path var/"log/mycorrhiza.log"
  end

  test do
    port = free_port
    with_test_process(bin/"mycorrhiza", "-listen-addr", "127.0.0.1:#{port}", ".") do
      sleep 5

      # Create a hypha
      cmd = "curl -siF'text=This is a test hypha.' 127.0.0.1:#{port}/upload-text/test_hypha"
      assert_match "303 See Other", shell_output(cmd)

      # Verify that it got created
      cmd = "curl -s 127.0.0.1:#{port}/hypha/test_hypha"
      assert_match "This is a test hypha.", shell_output(cmd)
    end
  end
end
