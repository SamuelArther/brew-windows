class Mklittlefs < Formula
  desc "Creates LittleFS images for ESP8266, ESP32, Pico RP2040, and RP2350"
  homepage "https://github.com/earlephilhower/mklittlefs"
  url "https://github.com/earlephilhower/mklittlefs/releases/download/4.1.0/x86_64-w64-mingw32-mklittlefs-42acb97.zip"
  sha256 "b57f64df7303651df2127b3032c33431dbf6a8c2e8949945ad588a2b6c6ee394"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/earlephilhower/mklittlefs/releases/download/4.1.0/mklittlefs-source.zip"
  end

  def install
    install_tree
    windows_exe "libexec/mklittlefs.exe", "mklittlefs"
  end

  test do
    mkdir (testpath/"data")
    (testpath/"data/hello.txt").write("Hello, World!")
    (testpath/"data/Poetry.txt").write("Had we but world enough, and time...")
    system(bin/"mklittlefs", "-s", "1048576", "-c", testpath/"data", testpath/"out.img")
    # mklittlefs unpacks into a path relative to the current directory
    system(bin/"mklittlefs", "-u", "out", testpath/"out.img")
    assert_equal "Hello, World!", (testpath/"out/hello.txt").read
    assert_equal "Had we but world enough, and time...", (testpath/"out/Poetry.txt").read
  end
end
