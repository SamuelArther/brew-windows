class Miasma < Formula
  desc "Trap AI web scrapers in an endless poison pit"
  homepage "https://github.com/austin-weeks/miasma"
  url "https://github.com/austin-weeks/miasma/releases/download/v0.4.3/miasma-windows-x86_64.exe"
  sha256 "44b1da5c8d9ed4e826e972349689a6e9aa0695c1c61bfa6ac5a4ceb297b02ebf"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/austin-weeks/miasma/archive/refs/tags/v0.4.3.tar.gz"
  end

  def install
    bin.install "miasma-windows-x86_64.exe" => "miasma.exe"
  end

  test do
    port = free_port
    pid = spawn bin/"miasma", "--host", "127.0.0.1", "--port", port.to_s

    # give the server a second to start up
    sleep 3
    system "curl", "-sSf", "http://127.0.0.1:#{port}/"
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
