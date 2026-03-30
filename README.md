# 🐧 PenguinSpace – Project Proposal

## Overview

**PenguinSpace** là một desktop application giúp developer **thu hồi (reclaim), tối ưu và giảm dung lượng ổ đĩa** bị chiếm bởi **WSL distros và Docker storage** trên Windows.

Khác với các tool monitor disk thông thường, PenguinSpace tập trung vào việc:

* Reset distro
* Repack distro
* Cleanup Docker storage
* Compact VHDX
* Thu hồi dung lượng ổ đĩa nhanh nhất có thể

Mục tiêu của PenguinSpace:

> Giúp developer reclaim disk space bị WSL và Docker “ăn mất” trên Windows.

---

# Problem / Pain Points

## Vấn đề thực tế khi dùng WSL + Docker

Rất nhiều developer trên Windows sử dụng:

* WSL
* Docker Desktop
* Node / Python / .NET
* Database containers
* Build artifacts
* Package cache

Sau một thời gian, ổ C bị đầy nhưng **không biết lý do và không biết dọn như thế nào**.

---

## Pain points chính

### 1. WSL ext4.vhdx phình to

WSL lưu toàn bộ filesystem trong file:

```text
ext4.vhdx
```

Khi xoá file trong Linux:

* Dung lượng trong Linux giảm
* Nhưng dung lượng Windows **không giảm**

Muốn shrink phải dùng:

```bash
wsl --shutdown
diskpart
compact vdisk
```

→ Rất thủ công, nhiều developer không biết.

---

### 2. Docker storage rất lớn

Docker images, layers, volumes nằm trong:

```text
docker-desktop-data ext4.vhdx
```

Dung lượng có thể lên:

* 30GB
* 50GB
* 100GB+

Nhưng Docker Desktop không có disk usage manager rõ ràng.

---

### 3. Cách giảm dung lượng hiệu quả nhất rất thủ công

Các cách reclaim disk space thực tế:

| Method                          | Hiệu quả |
| ------------------------------- | -------- |
| Xoá cache trong distro          | nhỏ      |
| docker prune                    | vừa      |
| compact vhdx                    | vừa      |
| export → import distro          | lớn      |
| unregister distro → install lại | rất lớn  |

Nhưng các bước này:

* Phải dùng CLI
* Phải nhớ nhiều lệnh
* Có nguy cơ mất dữ liệu nếu làm sai
* Không có UI
* Không có tool tổng hợp

---

### 4. Developer thường không biết distro nào đang chiếm dung lượng

Có thể có nhiều distro:

* Ubuntu
* Debian
* Kali
* docker-desktop
* docker-desktop-data

Windows không hiển thị size rõ ràng.

---

# Situation

Hiện tại:

* Windows không có tool quản lý WSL disk usage
* WSL CLI không có disk management tools
* Docker Desktop không có storage reclaim tool mạnh
* Shrink VHDX phải dùng diskpart thủ công
* Reset distro và repack distro phải dùng CLI
* Không có tool nào tập trung vào **reclaim disk space từ WSL và Docker**

=> Đây là một khoảng trống tool khá rõ ràng.

---

# Proposed Solution – PenguinSpace

## PenguinSpace sẽ giải quyết vấn đề bằng cách:

Cung cấp một desktop app để developer có thể:

* Xem distro nào đang chiếm dung lượng
* Reset distro
* Repack distro (export → import)
* Cleanup Docker storage
* Compact VHDX
* Shutdown WSL
* Thu hồi dung lượng ổ đĩa chỉ bằng vài nút bấm

### PenguinSpace không phải:

* WSL manager
* Docker manager
* Disk viewer

### PenguinSpace là:

> Tool để reclaim disk space từ WSL và Docker trên Windows.

---

# Core Features (v1)

## PenguinSpace v1 sẽ tập trung vào reclaim disk space

| Feature             | Mô tả                                        |
| ------------------- | -------------------------------------------- |
| List WSL distros    | Hiển thị Ubuntu, Debian, docker-desktop-data |
| Show ext4.vhdx size | Distro nào đang chiếm dung lượng             |
| Reset distro        | unregister + install lại                     |
| Repack distro       | export → unregister → import lại             |
| Docker cleanup      | docker system prune                          |
| Compact VHDX        | shrink ext4.vhdx                             |
| Shutdown WSL        | wsl --shutdown                               |
| Open distro folder  | Explorer                                     |
| Refresh             | Reload size                                  |

### Core Actions của PenguinSpace

| Action        | Mục tiêu                         |
| ------------- | -------------------------------- |
| Reset distro  | Xoá sạch distro để reclaim space |
| Repack distro | Shrink filesystem nhưng giữ data |
| Docker prune  | Xoá images/volumes               |
| Compact VHDX  | Shrink disk                      |
| Shutdown WSL  | Required step                    |

Đây là các tính năng cốt lõi của PenguinSpace.

---

# Technical Approach

PenguinSpace không cần kernel driver hay low-level integration.
Chỉ cần sử dụng:

* WSL CLI
* Docker CLI
* File system access
* Diskpart compact vdisk
* Desktop UI

## Các command PenguinSpace sẽ sử dụng

| Action         | Command                              |
| -------------- | ------------------------------------ |
| List distro    | `wsl -l -v`                          |
| Shutdown WSL   | `wsl --shutdown`                     |
| Reset distro   | `wsl --unregister` + `wsl --install` |
| Export distro  | `wsl --export`                       |
| Import distro  | `wsl --import`                       |
| Docker cleanup | `docker system prune -a`             |
| Compact vhdx   | `diskpart compact vdisk`             |
| Get size       | File system                          |

PenguinSpace thực chất là:

> UI + automation tool cho các WSL và Docker disk operations.

---

# Technical Architecture

## High-level architecture

```text
PenguinSpace Desktop App
 ├── UI Layer
 ├── WSL Service
 ├── Docker Service
 ├── Disk Service
 ├── Distro Service
 ├── Command Runner
 └── File System Analyzer
```

### Modules

| Module           | Responsibility        |
| ---------------- | --------------------- |
| WSL Service      | list distro, shutdown |
| Distro Service   | reset, export, import |
| Disk Service     | compact vhdx          |
| Docker Service   | prune images/volumes  |
| Storage Analyzer | disk usage            |
| Command Runner   | run CLI commands      |
| UI               | display dashboard     |

---

# Suggested Tech Stack

## Desktop App

| Option          | Tech                |
| --------------- | ------------------- |
| .NET            | WPF / WinUI         |
| Electron        | React + Node        |
| Tauri           | Rust + Frontend     |
| Avalonia        | Cross-platform .NET |
| Flutter Desktop | Dart                |

Nếu sử dụng .NET ecosystem:

> WPF / WinUI là lựa chọn phù hợp cho PenguinSpace.

---

# Roadmap

## Version 1 – Reclaim Space Tool

* List WSL distros
* Show ext4.vhdx size
* Reset distro
* Repack distro
* Docker prune
* Compact VHDX
* Shutdown WSL

## Version 2 – Storage Analyzer

* Show largest folders in distro
* Cleanup cache (apt, npm, pip, nuget)
* Docker images size
* Docker volumes size
* Disk usage breakdown

## Version 3 – Backup & Migration

* Export distro
* Import distro
* Clone distro
* Move distro to another drive
* Schedule backup

## Version 4 – Dev Environment Storage Manager

* Manage VM disks
* Manage container storage
* Dev environment cleanup
* Disk usage history
* Storage alerts

---

# Long-term Vision

PenguinSpace có thể evolve thành:

```text
Dev Storage Manager
 ├── WSL
 ├── Docker
 ├── VM disks
 ├── Dev caches
 ├── Package caches
 ├── Build artifacts
 └── Local development storage management
```

Mục tiêu dài hạn:

> PenguinSpace becomes the storage manager for developer environments on Windows.

# Summary

## PenguinSpace solves:

| Problem                    | Solution              |
| -------------------------- | --------------------- |
| WSL eats disk              | Reset / Repack distro |
| ext4.vhdx too big          | Compact VHDX          |
| Docker storage huge        | Docker cleanup        |
| Hard to reclaim disk space | UI automation         |
| Hard to manage distro      | Desktop tool          |
| Disk usage unclear         | Storage analyzer      |
