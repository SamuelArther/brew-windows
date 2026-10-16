class Nomino < Formula
  desc "Batch rename utility"
  homepage "https://github.com/yaa110/nomino"
  url "https://github.com/yaa110/nomino/releases/download/v1.6.4/nomino-windows-64bit.exe"
  sha256 "ea77b987adb4d802983ed73ada32e026cf92f8dfde23a35e2cc547e622b283b8"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/yaa110/nomino/archive/refs/tags/1.6.4.tar.gz"
  end

  def install
    bin.install "nomino-windows-64bit.exe" => "nomino.exe"
  end

  test do
    (1..9).each do |n|
      (testpath/"Homebrew-#{n}.txt").write n.to_s
    end

    system bin/"nomino", ".*-(\\d+).*", "{}"

    (1..9).each do |n|
      assert_equal n.to_s, (testpath/"#{n}.txt").read
      refute_path_exists testpath/"Homebrew-#{n}.txt"
    end
  end
end
