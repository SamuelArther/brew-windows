class Skm < Formula
  desc "Simple and powerful SSH keys manager"
  homepage "https://timothyye.github.io/skm"
  url "https://github.com/TimothyYe/skm/releases/download/v0.9.3/skm_0.9.3_windows_amd64.zip"
  sha256 "3e0da9814dc73d88605c8fc771178926a56550224920bb2e8758751c7902cfa6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/TimothyYe/skm/archive/refs/tags/v0.9.3.tar.gz"
  end

  def install
    bin.install "skm.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/skm --version")

    store = testpath/"store"
    ssh = testpath/"ssh"
    ssh.mkpath
    output = shell_output("#{bin}/skm --store-path #{store} --ssh-path #{ssh} init")
    assert_match "SSH key store initialized", output
    assert_predicate store, :directory?
  end
end
