class Pidof < Formula
  desc "Display the PID number for a given process name"
  # `nightproductions.net` is no longer accessible, use internet archive urls instead.
  homepage "https://web.archive.org/web/20240808152721/http://www.nightproductions.net/cli.htm"
  url "https://web.archive.org/web/20240808152721/http://www.nightproductions.net/downloads/pidof_source.tar.gz"
  mirror "https://distfiles.macports.org/pidof/pidof_source.tar.gz"
  version "0.1.4"
  sha256 "2a2cd618c7b9130e1a1d9be0210e786b85cbc9849c9b6f0cad9cbde31541e1b8"
  license :cannot_represent

  # `nightproductions.net` is no longer accessible
  deprecate! date: "2025-01-12", because: :repo_removed
  disable! date: "2026-01-12", because: :repo_removed

  # Hard dependency on sys/proc.h, which isn't available on Linux
  depends_on :macos

  def install
    # Fix "error: call to undeclared function 'strcasestr'" and "error: call to undeclared function 'kill'"
    # Contacted the upstream author via email on 2023-09-29
    inreplace "pidof.c",
              "#import <stdarg.h>\n",
              "#import <stdarg.h>\n#import <string.h>\n#import <signal.h>\n"

    system "make", "all", "CC=#{ENV.cc}", "CFLAGS=#{ENV.cflags}"
    man1.install Utils::Gzip.compress("pidof.1")
    bin.install "pidof"
  end

  test do
    assert_match "pidof version #{version}", shell_output("#{bin}/pidof -v")
    (testpath/"homebrew_testing.c").write <<~C
      #include <unistd.h>
      #include <stdio.h>

      int main()
      {
        printf("Testing Pidof\\n");
        sleep(10);
        return 0;
      }
    C
    system ENV.cc, "homebrew_testing.c", "-o", "homebrew_testing"
    (testpath/"homebrew_testing").chmod 0555

    pid = fork { exec "./homebrew_testing" }
    sleep 1
    begin
      assert_match(/\d+/, shell_output("#{bin}/pidof homebrew_testing"))
    ensure
      Process.kill("SIGINT", pid)
      Process.wait(pid)
    end
  end
end
