class Rustic < Formula
  desc "Fast, encrypted, and deduplicated backups powered by Rust"
  homepage "https://rustic.cli.rs"
  url "https://github.com/rustic-rs/rustic/releases/download/v0.11.4/rustic-v0.11.4-x86_64-pc-windows-gnu.tar.gz"
  sha256 "0052cf7bd84d512c58a64df02de81a1c47586a867fbf0764738f8fd0e06ea02e"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rustic-rs/rustic/archive/refs/tags/v0.11.4.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/rustic.exe", "rustic"
  end

  test do
    mkdir testpath/"rustic_repo"
    # rustic reads "C:/..." as a "C:" backend URL; a backslashed path is a local repository.
    ENV["RUSTIC_REPOSITORY"] = (testpath/"rustic_repo").to_s.tr("/", "\\")
    ENV["RUSTIC_PASSWORD"] = "test"

    (testpath/"testfile").write("test test test")

    system bin/"rustic", "init"
    system bin/"rustic", "backup", "testfile"

    system bin/"rustic", "restore", "latest:testfile", testpath/"testfile_restore"
    assert compare_file testpath/"testfile", testpath/"testfile_restore"
  end
end
