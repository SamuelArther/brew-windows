class Gotun < Formula
  desc "Lightweight HTTP proxy over SSH"
  homepage "https://github.com/Sesame2/gotun"
  url "https://github.com/Sesame2/gotun/archive/refs/tags/v0.8.1.tar.gz"
  sha256 "4448e9cd2efaf1ea5c1da50f39d3ea967632e15cffc36d2e1041f5c03b615750"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "076a23bf2c773bf924d650caefe5ae46155b491c1ca4d02ff9a152ebd0140c4c"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.Version=#{version}"), "./cmd/gotun"
    generate_completions_from_executable(bin/"gotun", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gotun --version")

    port = free_port
    server = TCPServer.new(port)
    server_pid = fork do
      msg = server.accept.gets
      server.close
      assert_match "SSH", msg
    end

    require "pty"
    r, _, pid = PTY.spawn bin/"gotun", "anonymous@localhost", "-p", port.to_s, "--timeout", "1s"
    sleep 1
    Process.kill "TERM", pid

    output = ""
    begin
      r.each_line { |line| output += line }
    rescue Errno::EIO
      # GNU/Linux raises EIO when read is done on closed pty
    end

    assert_match "GoTun dev", output
    assert_match "localhost:#{port}", output
  ensure
    Process.kill "TERM", server_pid
    Process.wait server_pid
    Process.wait pid
  end
end
