class Hyx < Formula
  desc "Powerful hex editor for the console"
  homepage "https://yx7.cc/code/"
  url "https://yx7.cc/code/hyx/hyx-2026.01.11.tar.xz"
  sha256 "550863c9e6a2c0e2618c16a562c8ee995e88c1d30e62abfdf4ecb819b3c4df54"
  license "MIT"

  livecheck do
    url :homepage
    regex(/href=.*?hyx[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e1bcd6adbf686269b4a0aa56736383dc2622c3fe2229ffa95f559bd4d11d12b8"
  end

  def install
    system "make"
    bin.install "hyx"
  end

  test do
    require "pty"
    PTY.spawn(bin/"hyx", "test") do |r, w, pid|
      r.winsize = [80, 43]
      w.write "62726577:wq\n"
      r.read
    rescue Errno::EIO
      # GNU/Linux raises EIO when read is done on closed pty
    ensure
      r.close
      w.close
      Process.wait(pid)
    end
    assert_equal "brew", (testpath/"test").read
  end
end
