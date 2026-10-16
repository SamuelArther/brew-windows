class Gmssl < Formula
  desc "Toolkit for Chinese national cryptographic standards"
  homepage "https://github.com/guanzhi/GmSSL"
  url "https://github.com/guanzhi/GmSSL/releases/download/v3.2.0/GmSSL-3.2.0-win64.zip"
  sha256 "d062923f09bfa74b06dbba74c4bda5e43a194d8aadec2ac82d723bbce0c5b7a5"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/guanzhi/GmSSL/archive/refs/tags/v3.2.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/bin/gmssl.exe", "gmssl"
  end

  test do
    expected_output = "ba7cc1a5be11d5f00dc8a88a9fedd74ccc9faf4655da08b7be3ae7e3954c76f1"
    output = pipe_output("#{bin}/gmssl sm3", "This is a test file").chomp
    assert_equal expected_output, output
  end
end
