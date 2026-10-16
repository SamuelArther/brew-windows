class BitGit < Formula
  desc "Bit is a modern Git CLI"
  homepage "https://github.com/chriswalz/bit"
  url "https://github.com/chriswalz/bit/releases/download/v1.1.2/bit_1.1.2_windows_amd64.tar.gz"
  sha256 "d9203188a9564c04c1cfc4f2985ca36124bcf2f4ab993a93fe05d6ee96e948f4"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/chriswalz/bit/archive/refs/tags/v1.1.2.tar.gz"
  end

  def install
    bin.install "bit.exe"
  end

  test do
    system "git", "init", testpath/"test-repository"

    cd testpath/"test-repository" do
      (testpath/"test-repository/test.txt").write <<~EOS
        Hello Homebrew!
      EOS
      system bin/"bit", "add", "test.txt"

      output = shell_output("#{bin}/bit status").chomp
      assert_equal "new file:   test.txt", output.lines.last.strip
    end
  end
end
