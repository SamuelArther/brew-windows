class JsonnetBundler < Formula
  desc "Package manager for Jsonnet"
  homepage "https://github.com/jsonnet-bundler/jsonnet-bundler"
  url "https://github.com/jsonnet-bundler/jsonnet-bundler/releases/download/v0.6.0/jb-windows-amd64.exe"
  sha256 "6af1455edfc92db1ace049e251df50ac04fcde497df3b98e716caf2652555984"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jsonnet-bundler/jsonnet-bundler.git"
  end

  def install
    bin.install "jb-windows-amd64.exe" => "jb.exe"
  end

  test do
    assert_match "A jsonnet package manager", shell_output("#{bin}/jb 2>&1")

    system bin/"jb", "init"
    assert_path_exists testpath/"jsonnetfile.json"

    system bin/"jb", "install", "https://github.com/grafana/grafonnet-lib"
    assert_predicate testpath/"vendor", :directory?
    assert_path_exists testpath/"jsonnetfile.lock.json"
  end
end
