class Curl < Formula
  desc "Get a file from an HTTP, HTTPS or FTP server"
  homepage "https://curl.se"
  version "8.23.0"
  url "https://curl.se/windows/dl-8.23.0_1/curl-8.23.0_1-win64-mingw.zip"
  sha256 "752ff19c45be64af5b5f8d5222dce64e0f7b03d740a7362a4055c220fa9637c6"
  license "curl"

  def install
    install_tree
    windows_exe "libexec/bin/curl.exe", "curl"
  end

  test do
    filename = testpath/"test.tar.gz"
    system bin/"curl", "-L", "https://curl.se/download/curl-#{version}.tar.gz", "-o", filename
    assert_path_exists filename

    assert_match version.to_s, shell_output("#{bin}/curl --version")
  end
end
