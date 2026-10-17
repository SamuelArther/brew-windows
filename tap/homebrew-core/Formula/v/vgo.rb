class Vgo < Formula
  desc "Project scaffolder for Go, written in Go"
  homepage "https://github.com/vg006/vgo"
  url "https://github.com/vg006/vgo/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "262ab18eb8e2f68031e17727c1c8b0e01e61c385dcd7addbd2c1ae86ecd312b4"
  license "MIT"
  head "https://github.com/vg006/vgo.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0fb150a0f67c9dc397749d0faa4681b9e04fc5921fa26c66fd23e3df27ed7ce8"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args

    generate_completions_from_executable(bin/"vgo", shell_parameter_format: :cobra)
  end

  test do
    require "pty"

    # vgo opens /dev/tty for its progress output, so give it a terminal of its own
    output = +""
    PTY.spawn(bin/"vgo", "build") do |r, _w, pid|
      begin
        r.each_line { |line| output << line }
      rescue Errno::EIO
        # GNU/Linux raises EIO when read is done on closed pty
      end
      Process.wait(pid)
    end
    assert_match "Failed to build the vgo tool", output
  end
end
