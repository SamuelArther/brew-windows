class Nginx < Formula
  desc "HTTP(S) server and reverse proxy, and IMAP/POP3 proxy server"
  homepage "https://nginx.org/"
  version "1.31.7"
  url "https://nginx.org/download/nginx-1.31.7.zip"
  sha256 "7c717ccf861f8925be64431d06d57634512a5014bcc8d149f279928f5e8151bf"
  license "BSD-2-Clause"

  # Official nginx.org Windows build. nginx/Windows resolves conf/, logs/ and temp/ against
  # the current directory, so the launcher passes -p <keg>/libexec/ (a later -p overrides it).

  def install
    install_tree
    windows_exe "libexec/nginx.exe", "nginx", "-p", "#{libexec}/"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nginx -v 2>&1")
    system bin/"nginx", "-t"
  end
end
