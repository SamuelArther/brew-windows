class SshCopyId < Formula
  desc "Add a public key to a remote machine's authorized_keys file"
  homepage "https://www.openssh.com/"
  url "https://cdn.openbsd.org/pub/OpenBSD/OpenSSH/portable/openssh-10.6p2.tar.gz"
  mirror "https://cloudflare.cdn.openbsd.org/pub/OpenBSD/OpenSSH/portable/openssh-10.6p2.tar.gz"
  version "10.6p2"
  sha256 "9ae44872090cf2f06b3cf9ff147e7f16f309671340763ed2f99248398ae29d5b"
  license "SSH-OpenSSH"
  head "https://github.com/openssh/openssh-portable.git", branch: "master"

  livecheck do
    formula "openssh"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "6581bf952f120aff84b1bd9b96e7b369d4af5be8ad93916c2f7cc00fb676f004"
  end

  keg_only :provided_by_macos

  deny_network_access!

  def install
    bin.install "contrib/ssh-copy-id"
    man1.install "contrib/ssh-copy-id.1"
  end

  test do
    output = shell_output("#{bin}/ssh-copy-id -h 2>&1", 1)
    assert_match "identity_file", output
  end
end
