class Killport < Formula
  desc "Command-line tool to kill processes listening on a specific port"
  homepage "https://github.com/jkfran/killport"
  url "https://github.com/jkfran/killport/releases/download/v2.0.2/killport-x86_64-pc-windows-gnu.tar.gz"
  sha256 "1e400414e16409fa9c154217982277ae77f66cc3a394a592221480b7ee1d6554"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jkfran/killport/archive/refs/tags/v2.0.2.tar.gz"
  end

  def install
    bin.install "killport.exe"
  end

  test do
    port = free_port
    output = shell_output("#{bin}/killport #{port}", 2)
    assert_match "No service found using port #{port}", output
  end
end
