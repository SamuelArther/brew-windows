class Piknik < Formula
  desc "Copy/paste anything over the network"
  homepage "https://github.com/jedisct1/piknik"
  url "https://github.com/jedisct1/piknik/releases/download/0.10.2/piknik-win64-0.10.2.zip"
  sha256 "4f9c7e0aa12971d988c00bf2e6a8926c9691c467d2fd21f264446aa8822208b0"
  license "BSD-2-Clause"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jedisct1/piknik/archive/refs/tags/0.10.2.tar.gz"
  end

  def install
    bin.install "piknik.exe"
  end

  test do
    conffile = testpath/"testconfig.toml"

    genkeys = shell_output("#{bin}/piknik -genkeys")
    lines = genkeys.lines.grep(/\s+=\s+/).map { |x| x.gsub(/\s+/, " ").gsub(/#.*/, "") }.uniq
    conffile.write lines.join("\n")
    pid = spawn bin/"piknik", "-server", "-config", conffile
    begin
      sleep 1
      pipe_output("#{bin}/piknik -config #{conffile} -copy", "test", 0)
      assert_equal "test", shell_output("#{bin}/piknik -config #{conffile} -move")
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
